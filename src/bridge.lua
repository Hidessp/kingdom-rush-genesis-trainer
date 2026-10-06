-- Runtime bridge for the local KR6 trainer. No external process memory writes.
local T = {damage_steps=0, cooldown_steps=0, ack='', message='ready'}
local base = love.filesystem.getSourceBaseDirectory()
local dir = base .. '/trainer-data/'
local bindings, offset, last_poll, last_status = {}, 0, -1, -1
local seen_damage = setmetatable({}, {__mode='k'})
local recent_sources = {}
local star_overrides = {}
local wrapped_save
local cooldown_records = setmetatable({}, {__mode='k'})
local wrapped_health, wrapped_utils, original_count_stars
local function read_file(name)
local f=io.open(dir..name,'rb'); if not f then return nil end
 local s=f:read('*a'); f:close(); return s
end
local function write_file(name,s)
 local f=io.open(dir..name,'wb'); if not f then return false end
 f:write(s);f:close();return true
end
local function write_history(s,name)
 name=name or 'stars-history.tsv'
 local tmp=dir..name..'.tmp'
 local target=dir..name
 local f=io.open(tmp,'wb');if not f then return false end
 local ok=f:write(s);f:close();if not ok then return false end
 local available,ffi=pcall(require,'ffi')
 if available then
  local worked,result=pcall(function()
   ffi.cdef('int __stdcall MoveFileExA(const char *oldName, const char *newName, unsigned long flags);')
   return ffi.C.MoveFileExA(tmp,target,9)~=0
  end)
  if worked then return result end
 end
 return os.rename(tmp,target)~=nil
end
local f=io.open(dir..'commands.tsv','rb')
if f then offset=f:seek('end') or 0;f:close() end
local function loaded(name) return package.loaded[name] end
local function store()
 local g=loaded('game');return type(g)=='table' and g.store or nil
end
local function slot()
 local s=loaded('storage')
 if type(s)~='table' or not s.active_slot_idx then return nil,s end
 return s:load_slot(),s
end
local function active_game(s)
 return type(s)=='table' and type(s.player_gold)=='number' and not s.game_outcome
end
local function source_is_tower(id,s,visited)
 if not id then return false end
 if recent_sources[id] then return true end
 visited=visited or {};if visited[id] then return false end;visited[id]=true
 local e=s.entities and s.entities[id];if not e or e.hero or e.enemy then return false end
 if e.tower then recent_sources[id]=true;return true end
 local owner=e.tower_ref
 if type(owner)=='table' and owner.tower then recent_sources[id]=true;return true end
 local candidates={e.tower_id,e.barrack_id,e.owner_id,e.spawner_id,e.source_id,
  e.bullet and e.bullet.source_id,e.modifier and e.modifier.source_id,e.aura and e.aura.source_id,
  type(owner)=='table' and owner.id or nil,e.soldier and e.soldier.tower_id}
 -- ipairs stops on nil, so examine every candidate index explicitly.
 for i=1,10 do
  if candidates[i] and source_is_tower(candidates[i],s,visited) then recent_sources[id]=true;return true end
 end
 return false
end
local function track_sources(s)
 if not s or type(s.entities)~='table' then return end
 for id,e in pairs(s.entities) do if e.tower and not e.enemy then recent_sources[id]=true end end
 for id,e in pairs(s.entities) do
  if e.soldier or e.bullet or e.modifier or e.aura then source_is_tower(id,s) end
 end
end
function T.scale_damage(s)
 if not s or type(s.damage_queue)~='table' then return end
 track_sources(s)
 local factor=1+T.damage_steps/10
 for _,d in pairs(s.damage_queue) do
  local target=s.entities and s.entities[d.target_id]
  if not seen_damage[d] and type(d.value)=='number' and d.value>0 and target and target.enemy
   and source_is_tower(d.source_id,s) then
   d.value=d.value*factor;seen_damage[d]=true
  end
 end
end
local function skill_key(k)
 if type(k)~='string' then return false end
 return k=='powers' or k=='timed_attacks' or k=='skills' or k=='ultimate' or k=='ult' or k:match('^skill_')~=nil
end
local function cooldown_key(k)
 if type(k)~='string' then return false end
 return k=='cooldown' or k=='cooldown_base' or k=='cooldown_inc' or k=='cooldown_min' or k=='cooldown_max'
  or k=='first_cooldown' or k=='starting_cooldown' or k=='shared_cooldown'
  or k=='instakill_cooldown' or k=='spawn_cooldown' or k=='ultimate_melee_cooldown'
end
local function scale_field(t,k,value,factor,derived)
 local rec=cooldown_records[t]
 if not rec then rec={};cooldown_records[t]=rec end
 local old=rec[k];local raw=value
 if old and value==old.scaled then raw=old.base
 elseif derived and derived[value] then raw=derived[value] end
 local scaled=raw*factor
 rec[k]={base=raw,scaled=scaled};t[k]=scaled
end
local function scale_skills(t,skill,factor,seen,path,derived)
 if type(t)~='table' or seen[t] then return end;seen[t]=true
 if t.hero or t.enemy then return end
 skill=skill or t.power_name~=nil or t.power~=nil
 for k,v in pairs(t) do
  if type(v)=='number' and skill and cooldown_key(k) and v>=0 then
   scale_field(t,k,v,factor,k=='cooldown' and derived or nil)
  elseif type(v)=='table' and k~='tower_ref' and k~='render' and k~='ui' and k~='nav_path' and k~='health' then
   local child_skill=skill or skill_key(k)
   if skill and cooldown_key(k) then
    for index,number in pairs(v) do if type(number)=='number' and number>=0 then scale_field(v,index,number,factor) end end
   end
   -- KR6 tower attacks: slot 1 is the ordinary attack, the rest are skills.
   if path=='attacks.list' and type(k)=='number' and k>1 then child_skill=true end
   local child_path=tostring(k)
   if k=='list' and path=='attacks' then child_path='attacks.list' end
   scale_skills(v,child_skill,factor,seen,child_path,derived)
  end
 end
end
local function scale_entity_skills(e,factor)
 local derived={}
 local function capture(t,seen)
  if type(t)~='table' or seen[t] then return end;seen[t]=true
  for k,v in pairs(t) do
   if cooldown_key(k) then
    if type(v)=='number' then
     local rec=cooldown_records[t] and cooldown_records[t][k]
     local raw=rec and v==rec.scaled and rec.base or v
     if rec then derived[rec.scaled]=rec.base end
     derived[raw*factor]=raw
    elseif type(v)=='table' then
     for index,n in pairs(v) do if type(n)=='number' then
      local rec=cooldown_records[v] and cooldown_records[v][index]
      local raw=rec and n==rec.scaled and rec.base or n
      if rec then derived[rec.scaled]=rec.base end
      derived[raw*factor]=raw
     end end
    end
   elseif type(v)=='table' then capture(v,seen) end
  end
 end
 capture(e.powers,{})
 -- Scale power configuration before its derived attack timers.
 local seen={}
 if e.powers then scale_skills(e.powers,true,factor,seen,'powers') end
 scale_skills(e,false,factor,seen,'',derived)
end
function T.scale_cooldowns(s)
 if not s or type(s.entities)~='table' then return end
 local factor=1-T.cooldown_steps/10
 for _,e in pairs(s.entities) do
  if e.tower and not e.enemy then scale_entity_skills(e,factor)
  elseif e.soldier and not e.hero and source_is_tower(e.id,s) then scale_entity_skills(e,factor) end
 end
end
local function get_stars()
 local sl=slot();if not sl then return nil end
 local u=loaded('utils')
 if type(u)=='table' and type(u.count_stars)=='function' then return u.count_stars(sl) end
 return nil
end
-- Additional resources: unspent points, per-unit offsets, and barracks counts.
local resource_edits, point_originals, point_wrappers = {}, {}, {}
local barracks_originals=setmetatable({}, {__mode='k'})
local template_defaults={}
local resource_actions={settowerpoints=true,restoretowerpoints=true,setheropoints=true,setpowerpoints=true,setbarracks=true,resetbarracks=true}
local function point_data(sl)
 return sl and sl.codex_trainer_points or {}
end
local function point_offset(kind,id)
 local sl=slot();local data=point_data(sl)
 if kind=='tower' then return data.tower or 0 end
 return (data[kind] or {})[id] or 0
end
local function point_view(kind)
 local name=kind=='tower' and 'upgrades_room_view' or kind=='heroes' and 'hero_room_view' or 'power_room_view'
 local candidates={_G.screen_map,loaded('screen_map')}
 for i=1,2 do
  local m=candidates[i]
  if type(m)=='table' and type(m[name])=='table' then return m[name] end
 end
 return nil
end
function T.points_hooks()
 local defs={tower={class='UpgradesRoomView',method='available_points'},heroes={class='HeroRoomView',method='get_unit_stats'},powers={class='PowerRoomView',method='get_unit_stats'}}
 for kind,def in pairs(defs) do
  local cls=_G[def.class]
  if type(cls)=='table' and type(cls[def.method])=='function' and cls[def.method]~=point_wrappers[kind] then
   local original=cls[def.method];point_originals[kind]=original
   local wrapper
   if kind=='tower' then
    wrapper=function(self,...)
     return math.max(0,original(self,...)+point_offset('tower'))
    end
   else
    local resource_kind=kind
    wrapper=function(self,id,...)
     local info=original(self,id,...)
     local unit=id or self.unit_shown
     if type(info)=='table' and type(info.remaining_points)=='number' then
      info.remaining_points=math.max(0,info.remaining_points+point_offset(resource_kind,unit))
     end
     return info
    end
   end
   point_wrappers[kind]=wrapper;cls[def.method]=wrapper
  end
 end
end
local function point_state(kind,raw)
 local view=point_view(kind)
 if type(view)~='table' then return nil end
 local method=raw and point_originals[kind] or (kind=='tower' and view.available_points or view.get_unit_stats)
 if type(method)~='function' then return nil end
 if kind=='tower' then
  local ok,n=pcall(method,view)
  if ok and type(n)=='number' then return {points=n,view=view} end
 else
  local id=view.unit_shown
  if type(id)~='string' or id=='' then return nil end
  local sl=slot();local statuses=sl and sl[kind] and sl[kind].status
  if type(statuses)~='table' or not statuses[id] then return nil end
  local ok,info=pcall(method,view,id)
  if ok and type(info)=='table' and type(info.remaining_points)=='number' then
   return {id=id,name=info.unit_name or id,points=info.remaining_points,view=view}
  end
 end
 return nil
end
function T.apply_resource_edits(sl,number)
 local edits=resource_edits[number];if not edits or type(sl)~='table' then return end
 sl.codex_trainer_points=sl.codex_trainer_points or {}
 local data=sl.codex_trainer_points
 if edits.tower~=nil then data.tower=edits.tower end
 for _,kind in ipairs({'heroes','powers'}) do
  if edits[kind] then
   data[kind]=data[kind] or {}
   for id,n in pairs(edits[kind]) do data[kind][id]=n end
  end
 end
end
local function refresh_points(kind,view)
 if kind=='tower' and type(view.refresh_trees)=='function' then pcall(view.refresh_trees,view)
 elseif type(view.refresh_upgrades)=='function' then pcall(view.refresh_upgrades,view) end
 if kind~='tower' and type(view.set_upgrade_points_label)=='function' then
  local info=point_state(kind,false)
  if info then pcall(view.set_upgrade_points_label,view,info.points) end
 end
end
local function saved_points(kind,id,n)
 local sl,st=slot();if not sl then return false,'select_slot_first' end
 T.points_hooks()
 local old=point_state(kind,false);local normal=point_state(kind,true)
 if not old or not normal then return false,'open_'..kind..'_points_first' end
 if kind~='tower' and old.id~=id then return false,'unit_changed' end
 local backupname='slot_'..tostring(st.active_slot_idx)..'_before_points.lua'
 if not read_file(backupname) then
  if type(st.serialize_lua)~='function' then return false,'backup_failed' end
  local ok,serialized=pcall(st.serialize_lua,st,sl)
  if not ok or type(serialized)~='string' or not write_file(backupname,serialized) then return false,'backup_failed' end
 end
 local history,historyname
 if kind=='tower' then
  historyname='tower-points-history.tsv';history=read_file(historyname) or '';local seq=0
  for index in history:gmatch('(%d+)\t%d+\t%d+\t%d+[\r\n]+') do seq=math.max(seq,tonumber(index)) end
  local line=string.format('%d\t%d\t%d\t%d\n',seq+1,st.active_slot_idx,old.points,n)
  if not write_history(history..line,historyname) then return false,'history_write_failed' end
 end
 local data=sl.codex_trainer_points or {};sl.codex_trainer_points=data
 local shadow=resource_edits[st.active_slot_idx] or {};resource_edits[st.active_slot_idx]=shadow
 local offset=n-normal.points
 local previous,shadowprevious
 if kind=='tower' then previous=data.tower;shadowprevious=shadow.tower;data.tower=offset;shadow.tower=offset
 else
  data[kind]=data[kind] or {};shadow[kind]=shadow[kind] or {}
  previous=data[kind][id];shadowprevious=shadow[kind][id];data[kind][id]=offset;shadow[kind][id]=offset
 end
 local ok,saved=pcall(st.save_slot,st,sl)
 if not ok or saved==false then
  if kind=='tower' then data.tower=previous;shadow.tower=shadowprevious
  else data[kind][id]=previous;shadow[kind][id]=shadowprevious end
  if history then write_history(history,historyname) end
  return false,'save_failed'
 end
 refresh_points(kind,old.view)
 return true,kind..'_points_saved'
end
local function remove_extra_soldiers(owner,limit,s)
 local lists={owner.barrack and owner.barrack.soldiers,owner.soldiers}
 for i=1,2 do
  local list=lists[i]
  if type(list)=='table' then
   for index,e in pairs(list) do
    if type(index)=='number' and index>limit then
     local unit=type(e)=='table' and e or s.entities and s.entities[e]
     if unit and unit.soldier and not unit.hero then
      local game=loaded('game');local sim=game and game.simulation
      if sim and type(sim.queue_remove_entity)=='function' then sim:queue_remove_entity(unit)
      elseif type(s.pending_removals)=='table' and not unit.pending_removal then
       unit.pending_removal=true;s.pending_removals[#s.pending_removals+1]=unit
      end
      list[index]=nil
     end
    end
   end
  end
 end
end
local function barrack_fields(e,callback)
 if not e or e.enemy or e.hero then return end
 if e.barrack and type(e.barrack.max_soldiers)=='number' then callback(e.barrack,'max_soldiers','barrack') end
 if e.tower and type(e.max_soldiers)=='number' then callback(e,'max_soldiers','max') end
 if type(e.barracks_soldiers)=='table' then
  for k,v in pairs(e.barracks_soldiers) do if type(v)=='number' then callback(e.barracks_soldiers,k,'camp') end end
 end
end
function T.apply_barracks(s)
 local E=loaded('entity_db')
 if type(E)=='table' and type(E.entities)=='table' then
  for name,e in pairs(E.entities) do
   barrack_fields(e,function(t,k,field)
    local rec=barracks_originals[t]
    if not rec then rec={};barracks_originals[t]=rec end
    if rec[k]==nil then rec[k]=t[k] end
    template_defaults[name]=template_defaults[name] or {};template_defaults[name][field..':'..tostring(k)]=rec[k]
    t[k]=T.barracks_count or rec[k]
   end)
  end
 end
 if not s or type(s.entities)~='table' then return end
 for _,e in pairs(s.entities) do
  barrack_fields(e,function(t,k,field)
   local rec=barracks_originals[t]
   if not rec then rec={};barracks_originals[t]=rec end
   local vanilla=(template_defaults[e.template_name] or {})[field..':'..tostring(k)]
   if rec[k]==nil then rec[k]=((t[k]==T.barracks_count or t[k]==T.previous_barracks_count) and vanilla) or t[k] end
   rec._last=rec._last or {}
   if T.barracks_count and rec._last[k] and t[k]~=rec._last[k] and t[k]~=T.barracks_count then rec[k]=t[k] end
   t[k]=T.barracks_count or rec[k];rec._last[k]=t[k]
   if field=='barrack' or field=='max' then remove_extra_soldiers(e,t[k],s) end
  end)
 end
end
function T.resource_action(action,value)
 if action=='setbarracks' then
  local n=tonumber(value)
  if not n or n~=math.floor(n) or n<1 or n>50 then return false,'invalid_barracks' end
  T.previous_barracks_count=T.barracks_count;T.barracks_count=n;T.apply_barracks(store());T.previous_barracks_count=nil;return true,'barracks_updated'
 elseif action=='resetbarracks' then
  T.previous_barracks_count=T.barracks_count;T.barracks_count=nil;T.apply_barracks(store());T.previous_barracks_count=nil;return true,'barracks_reset'
 elseif action=='restoretowerpoints' then
  local st=loaded('storage');local wanted=tonumber(value)
  for id,index,previous in (read_file('tower-points-history.tsv') or ''):gmatch('(%d+)\t(%d+)\t(%d+)\t%d+[\r\n]+') do
   if tonumber(id)==wanted then
    if not st or tonumber(index)~=st.active_slot_idx then return false,'history_wrong_slot' end
    return saved_points('tower',nil,tonumber(previous))
   end
  end
  return false,'history_missing'
 else
  local kind=action=='settowerpoints' and 'tower' or action=='setheropoints' and 'heroes' or 'powers'
  local id,n
  if kind=='tower' then n=tonumber(value) else
   local text=value or '';local number;id,number=text:match('^([%w_]+),(%d+)$');n=tonumber(number)
  end
  if not n or n~=math.floor(n) or n<0 or n>999999 then return false,'invalid_points' end
  return saved_points(kind,id,n)
 end
end
function T.resource_status()
 local rows={}
 for _,kind in ipairs({'tower','heroes','powers'}) do
  local info=point_state(kind,false)
  rows[#rows+1]=kind..'_points\t'..tostring(info and info.points or '')
  if kind~='tower' then
   rows[#rows+1]=kind..'_id\t'..tostring(info and info.id or '')
   rows[#rows+1]=kind..'_name\t'..tostring(info and info.name or ''):gsub('[\r\n\t]',' ')
  end
 end
 rows[#rows+1]='barracks_count\t'..tostring(T.barracks_count or '')
 return table.concat(rows,'\n')
end

local function hooks()
 T.points_hooks()
 local st=loaded('storage')
 if type(st)=='table' and type(st.save_slot)=='function' and st.save_slot~=wrapped_save then
  local old=st.save_slot
  wrapped_save=function(self,sl,idx,...)
   local number=idx or self.active_slot_idx
   T.apply_resource_edits(sl,number)
   if type(sl)=='table' and star_overrides[number]~=nil then sl.codex_trainer_star_adjustment=star_overrides[number] end
   return old(self,sl,idx,...)
  end
  st.save_slot=wrapped_save
 end
 local sys=loaded('systems')
 if type(sys)=='table' and type(sys.health)=='table' and type(sys.health.on_update)=='function'
  and sys.health.on_update~=wrapped_health then
  local old=sys.health.on_update
  wrapped_health=function(...)
   for i=1,select('#',...) do
    local v=select(i,...)
    if type(v)=='table' and v.damage_queue and v.entities then T.scale_damage(v);break end
   end
   return old(...)
  end
  sys.health.on_update=wrapped_health
 end
 local u=loaded('utils')
 if type(u)=='table' and type(u.count_stars)=='function' and u.count_stars~=wrapped_utils then
  local old=u.count_stars
  original_count_stars=old
  wrapped_utils=function(sl,...)
   local normal=old(sl,...)
   if type(sl)=='table' and type(sl.codex_trainer_star_adjustment)=='number' then
    return math.max(0,normal+sl.codex_trainer_star_adjustment)
   end
   return normal
  end
  u.count_stars=wrapped_utils
 end
end
local function refresh_map(n)
 local m=loaded('screen_map')
 if type(m)=='table' then
  m.total_stars=n
  if m.window and type(m.window.ci)=='function' then
   local label=m.window:ci('label_map_stars');if label then label.text=tostring(n) end
  end
 end
end
function T.action(action,value)
 if resource_actions[action] then return T.resource_action(action,value) end
 local s=store()
 if action=='add500' or action=='add100' or action=='setgold' then
  if not active_game(s) then return false,'enter_level_first' end
  if action=='add500' then s.player_gold=math.min(999999999,s.player_gold+500)
  elseif action=='add100' then s.player_gold=math.min(999999999,s.player_gold+100)
  else
   local n=tonumber(value);if not n or n<0 or n>999999999 or n~=math.floor(n) then return false,'invalid_gold' end
   s.player_gold=n
  end
  return true,'gold_updated'
 elseif action=='damage' then
  T.damage_steps=T.damage_steps+1;return true,'damage_updated'
 elseif action=='cooldown' then
  if T.cooldown_steps>=9 then return false,'cooldown_limit' end
  T.cooldown_steps=T.cooldown_steps+1;T.scale_cooldowns(s);return true,'cooldown_updated'
 elseif action=='restorestars' then
  local st=loaded('storage');local wanted=tonumber(value)
  local history=read_file('stars-history.tsv') or ''
  for id,index,previous in history:gmatch('(%d+)\t(%d+)\t(%d+)\t%d+[\r\n]+') do
   if tonumber(id)==wanted then
    if not st or tonumber(index)~=st.active_slot_idx then return false,'history_wrong_slot' end
    return T.action('setstars',previous)
   end
  end
  return false,'history_missing'
 elseif action=='setstars' then
  local n=tonumber(value);if not n or n<0 or n>999999 or n~=math.floor(n) then return false,'invalid_stars' end
  local sl,st=slot();if not sl then return false,'select_slot_first' end
  -- Keep a one-time pre-edit snapshot, through the game's own serializer.
  local backupname='slot_'..tostring(st.active_slot_idx)..'_before_stars.lua'
  if not read_file(backupname) and type(st.serialize_lua)=='function' then
   local serialized=st:serialize_lua(sl)
   if type(serialized)=='string' and not write_file(backupname,serialized) then return false,'backup_failed' end
  end
  local u=loaded('utils')
  local prior=type(u)=='table' and u.count_stars(sl) or nil
  if type(prior)~='number' then return false,'stars_unavailable' end
  local history=read_file('stars-history.tsv') or '';local sequence=0
  for id in history:gmatch('(%d+)\t%d+\t%d+\t%d+[\r\n]+') do sequence=math.max(sequence,tonumber(id)) end
  local line=string.format('%d\t%d\t%d\t%d\n',sequence+1,st.active_slot_idx,prior,n)
  if not write_history(history..line) then return false,'history_write_failed' end
  local previous=sl.codex_trainer_star_adjustment;local old_override=star_overrides[st.active_slot_idx]
  local vanilla=original_count_stars and original_count_stars(sl) or prior-(previous or 0)
  sl.codex_trainer_star_adjustment=n-vanilla;star_overrides[st.active_slot_idx]=n-vanilla
  local call_ok,saved=pcall(st.save_slot,st,sl)
  if not call_ok or saved==false then
   sl.codex_trainer_star_adjustment=previous;star_overrides[st.active_slot_idx]=old_override
   write_history(history);return false,'save_failed'
  end
  refresh_map(n);return true,'stars_saved_reopen_map'
 end
 return false,'unknown_action'
end
local function poll_commands()
 local f=io.open(dir..'commands.tsv','rb');if not f then return end
 local size=f:seek('end') or 0
 if size<offset then offset=0 end
 f:seek('set',offset)
 while true do
  local start=f:seek();local line=f:read('*l');if not line then break end
  -- A complete journal entry always ends with a newline.
  local finish=f:seek();f:seek('set',finish-1);local ending=f:read(1);f:seek('set',finish)
  if ending~='\n' then f:seek('set',start);break end
  local id,action,value=line:match('^([^\t]+)\t([^\t]+)\t(.*)$')
  if id then
   local ok,result,msg=pcall(T.action,action,value)
   T.ack=id;T.message=ok and msg or ('bridge_error:'..tostring(result))
   T.last_ok=ok and result or false
  end
  offset=finish
 end
 f:close()
end
local function poll_bindings()
 local data=read_file('settings.ini');if not data then return end
 local next_bindings={}
 for key,value in data:gmatch('([^\r\n=]+)=([^\r\n]*)') do
  if key:match('^hotkey_') and value~='' then next_bindings[key:sub(8)]=value:lower() end
 end
 bindings=next_bindings
end
local last_store,last_entities
function T.tick()
 hooks()
 local s=store()
 if s~=last_store or (s and s.entities~=last_entities) then
  recent_sources={};cooldown_records=setmetatable({}, {__mode='k'});last_store=s;last_entities=s and s.entities
 end
 track_sources(s);T.scale_cooldowns(s)
 if T.barracks_count then T.apply_barracks(s) end
 local now=love.timer.getTime()
 if now-last_poll>=0.10 then poll_commands();poll_bindings();last_poll=now end
 if now-last_status>=0.25 then
  local n=get_stars()
  write_file('status.tsv',table.concat({
   'protocol\t1','pid\t'..tostring(T.pid or 0),'gold\t'..tostring(active_game(s) and s.player_gold or ''),
   'stars\t'..tostring(n or ''),'slot\t'..tostring((loaded('storage') or {}).active_slot_idx or ''),
   'damage\t'..string.format('%.1f',1+T.damage_steps/10),
   'cooldown\t'..string.format('%.1f',1-T.cooldown_steps/10),
   'cooldown_steps\t'..T.cooldown_steps,'ack\t'..T.ack,'ok\t'..tostring(T.last_ok~=false),
   'message\t'..T.message,'health_hook\t'..tostring(wrapped_health~=nil),T.resource_status()},'\n')..'\n')
  last_status=now
 end
end
local function pressed_combo(key)
 local kb=love.keyboard;local a={}
 if kb.isDown('lctrl','rctrl') then a[#a+1]='ctrl' end
 if kb.isDown('lalt','ralt') then a[#a+1]='alt' end
 if kb.isDown('lshift','rshift') then a[#a+1]='shift' end
 a[#a+1]=key:lower();return table.concat(a,'+')
end
function T.keypressed(key,isrepeat)
 if isrepeat then return false end
 local combo=pressed_combo(key)
 for action,binding in pairs(bindings) do
  if combo==binding then
   local ok,result,msg=pcall(T.action,action)
   T.message=ok and msg or ('bridge_error:'..tostring(result));T.last_ok=ok and result or false
   return true
  end
 end
 return false
end
function T.install()
 local ok,ffi=pcall(require,'ffi')
 if ok then
  pcall(function() ffi.cdef('unsigned long __stdcall GetCurrentProcessId(void);');T.pid=tonumber(ffi.C.GetCurrentProcessId()) end)
 end
 local old_update=love.update
 love.update=function(...)
  local ok,err=pcall(T.tick);if not ok then T.message='bridge_error:'..tostring(err);write_file('error.log',T.message) end
  if old_update then return old_update(...) end
 end
 local old_keypressed=love.keypressed
 love.keypressed=function(key,...)
  -- LÖVE versions use either (key, repeat) or (key, scancode, repeat).
  local a,b=...;local repeated=type(b)=='boolean' and b or type(a)=='boolean' and a or false
  if T.keypressed(key,repeated) then return end
  if old_keypressed then return old_keypressed(key,...) end
 end
 poll_bindings();hooks()
end
return T
