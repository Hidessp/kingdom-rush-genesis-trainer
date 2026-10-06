# Installer for a user-owned game copy; no game code or assets are distributed.
import copy,ctypes,os,zipfile
from pathlib import Path
import argparse,shutil,sys
parser=argparse.ArgumentParser(description='Install / uninstall the Kingdom Rush Genesis trainer bridge.')
parser.add_argument('--game-dir',type=Path,default=Path(__file__).resolve().parent)
parser.add_argument('--uninstall',action='store_true')
args=parser.parse_args()
root=args.game_dir.resolve()
output=root/'Kingdom Rush Genesis.exe'
backup=root/'.kr6-trainer-backup'
baseline=backup/'Kingdom Rush Genesis.original.exe'
if not output.is_file():sys.exit('Game executable not found. Extract the package into your game directory or use --game-dir.')
if args.uninstall:
 if not baseline.is_file():sys.exit('Original backup not found; uninstall cancelled.')
 shutil.copy2(baseline,output)
 print('Original game restored. Your saves and trainer history were kept.')
 sys.exit(0)
if not (root/'lua51.dll').is_file():sys.exit('lua51.dll missing. This package supports the Windows LuaJIT build of Kingdom Rush Genesis.')
with zipfile.ZipFile(output) as game:
 for name in ['main.lua','all/components.lua','kr6/templates_game.lua','kr6/data/balance/balance.lua']:
  if name not in game.namelist():sys.exit('Unsupported game build: missing '+name)
 if not baseline.exists():
  if 'codex_trainer/main_original.luac' in game.namelist():sys.exit('A trainer is already installed but its original backup is missing. Restore your original game before installing this package.')
  backup.mkdir(exist_ok=True)
  shutil.copy2(output,baseline)
temp=backup/'Kingdom Rush Genesis.install.tmp'
wrapper=b'''local original = assert(love.filesystem.load("codex_trainer/main_original.luac"))\noriginal()\nlocal ok, err = pcall(function()\n local bridge = assert(love.filesystem.load("codex_trainer/bridge.lua"))()\n bridge.install()\nend)\nif not ok then print("KR6 trainer startup: " .. tostring(err)) end\n'''
bridge=(Path(__file__).resolve().parent/'src/bridge.lua').read_text(encoding='utf-8-sig').encode('utf-8')
lib=ctypes.CDLL(str(root/'lua51.dll'));lib.luaL_newstate.restype=ctypes.c_void_p
lib.luaL_loadbuffer.argtypes=[ctypes.c_void_p,ctypes.c_char_p,ctypes.c_size_t,ctypes.c_char_p]
lib.lua_tolstring.argtypes=[ctypes.c_void_p,ctypes.c_int,ctypes.c_void_p];lib.lua_tolstring.restype=ctypes.c_char_p
lib.lua_close.argtypes=[ctypes.c_void_p]
for name,data in [('main.lua',wrapper),('bridge.lua',bridge)]:
 L=lib.luaL_newstate();rc=lib.luaL_loadbuffer(L,data,len(data),name.encode());assert rc==0,lib.lua_tolstring(L,-1,None);lib.lua_close(L)
with zipfile.ZipFile(baseline) as src:
 infos=sorted(src.infolist(),key=lambda t:t.header_offset);prefix=infos[0].header_offset
 with baseline.open('rb') as inp,temp.open('wb') as out:
  out.write(inp.read(prefix))
  with zipfile.ZipFile(out,'w',allowZip64=True) as dst:
   dst.comment=src.comment
   for index,info in enumerate(infos):
    if info.filename=='main.lua':dst.writestr(copy.copy(info),wrapper)
    else:
     cloned=copy.copy(info);cloned.header_offset=out.tell()
     end=infos[index+1].header_offset if index+1<len(infos) else src.start_dir
     inp.seek(info.header_offset);remaining=end-info.header_offset
     while remaining:
      chunk=inp.read(min(remaining,8*1024*1024));assert chunk;out.write(chunk);remaining-=len(chunk)
     dst.filelist.append(cloned);dst.NameToInfo[cloned.filename]=cloned;dst.start_dir=out.tell();dst._didModify=True
   dst.writestr('codex_trainer/main_original.luac',src.read('main.lua'),compress_type=zipfile.ZIP_DEFLATED)
   dst.writestr('codex_trainer/bridge.lua',bridge,compress_type=zipfile.ZIP_DEFLATED)
 with zipfile.ZipFile(temp) as check:
  for info in src.infolist():
   if info.filename!='main.lua':
    new=check.getinfo(info.filename);assert (new.CRC,new.file_size,new.compress_size)==(info.CRC,info.file_size,info.compress_size)
  for name in ['all/components.lua','kr6/templates_game.lua','kr6/data/balance/balance.lua']:
   assert check.read(name)==src.read(name),'Barracks restoration failed'
  assert check.read('codex_trainer/bridge.lua')==bridge
os.replace(temp,output)
print('Trainer bridge installed. Start the game normally and open KR6Trainer.exe. / 安装完成，请按原方式启动游戏并打开修改器。')
