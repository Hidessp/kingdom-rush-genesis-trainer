using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;

namespace KR6Trainer {
 public sealed class Trainer : Form {
  static bool English;
  static readonly Dictionary<string,string> EnglishText = new Dictionary<string,string> {
{"游戏已连接","Game connected"},
{"金钱已修改","Gold updated"},
{"伤害倍率已增加 0.1","Tower damage increased by 0.1x"},
{"技能冷却倍率已降低 0.1","Skill cooldown reduced by 0.1x"},
{"冷却已达到最低 0.1 倍（9 次上限）","Minimum cooldown reached: 0.1x (9 steps)"},
{"请先进入关卡，再修改金钱","Enter a level before editing gold"},
{"请先在游戏中选择存档","Select a save slot in the game first"},
{"星星已保存；重新进入地图或解锁页面刷新","Stars saved; reopen the map or unlock screen to refresh"},
{"金钱必须是 0～999999999 的整数","Gold must be an integer from 0 to 999999999"},
{"星星必须是 0～999999 的整数","Stars must be an integer from 0 to 999999"},
{"存档写入失败，星星修改未保存","Save failed; changes were not saved"},
{"存档备份失败，星星修改已取消","Backup failed; changes were cancelled"},
{"这条记录属于其他存档，请先切换到对应存档","This record belongs to another save slot"},
{"所选历史记录不存在，请刷新后重试","History record missing; refresh and retry"},
{"无法保存星星历史，本次修改已取消","Could not save history; changes were cancelled"},
{"无法读取当前星星数量，请先进入地图","Open the map to read your stars"},
{"防御塔升级点数已保存，修改前的点数已加入历史","Tower points saved; previous points added to history"},
{"当前英雄的升级点数已保存","Selected hero's points saved"},
{"当前法术的升级点数已保存","Selected spell's points saved"},
{"请在游戏中打开防御塔的全局升级页面","Open the game's global tower upgrades screen"},
{"请在游戏中打开英雄页面并选择英雄","Open the hero upgrade screen and select a hero"},
{"请在游戏中打开法术页面并选择法术","Open the spell upgrade screen and select a spell"},
{"游戏中当前选择已发生变化，请确认名称后重试","Selection changed; check the selected unit and retry"},
{"升级点数必须是 0～999999 的整数","Points must be an integer from 0 to 999999"},
{"兵营士兵数量必须是 1～50 的整数","Soldier count must be an integer from 1 to 50"},
{"兵营数量已设置；新士兵将按兵营的召唤流程补齐","Count updated; barracks will summon additional soldiers"},
{"兵营数量已重置为各兵营的游戏原值","Each barracks restored to its original soldier count"},
{"第 ","#"},
{" 次编辑前："," before edit: "},
{" 点"," points"},
{" 星"," stars"},
{"  ·  存档 ","  / Slot "},
{"王国保卫战 6 · 修改器","Kingdom Rush Genesis Trainer"},
{"王国保卫战 6","Kingdom Rush Genesis"},
{"实时修改器  /  升级点数、兵营数量与历史恢复","Live trainer / Upgrade points, barracks and restore history"},
{"窗口置顶","Always on top"},
{"金钱 +500","Gold +500"},
{"金钱 +100","Gold +100"},
{"防御塔伤害倍率 +0.1","Tower damage +0.1x"},
{"防御塔技能冷却倍率 −0.1","Tower cooldown -0.1x"},
{"启用快捷键","Use hotkey"},
{"点击后按键","Press a key"},
{"伤害倍率：等待连接","Damage: awaiting game"},
{"技能冷却倍率：等待连接","Cooldown: awaiting game"},
{"保存快捷键设置","Save hotkeys"},
{"快捷键在游戏窗口中生效；保存后的设置会保留。","Hotkeys work in the game window. Settings are saved."},
{"解锁星星：等待存档","Unlock stars: awaiting save"},
{"关卡金钱：等待进入关卡","Gold: enter a level"},
{"设置星星","Set stars"},
{"设置金钱","Set gold"},
{"使用原来的启动方式打开游戏，再选择存档或进入关卡。","Start the game normally, then select a save or enter a level."},
{"星星修改历史（按编辑顺序，记录修改前的数量）","Star history (ordered; values before each edit)"},
{"恢复选中记录","Restore selection"},
{"请先选择一条历史记录。","Select a history record first."},
{"战斗与星星","Battle & Stars"},
{"升级点数与兵营","Points & Barracks"},
{"防御塔升级点数：等待升级页面","Tower points: open upgrades"},
{"设置塔升级点数","Set tower points"},
{"防御塔升级点数历史（记录每次修改前的可用点数）","Tower point history (available points before each edit)"},
{"恢复选中点数","Restore points"},
{"请先选择一条升级点数历史记录。","Select a point history record first."},
{"当前英雄：请在游戏中选择英雄","Hero: select a hero in the game"},
{"英雄升级点数（每名英雄独立）","Hero points (independent per hero)"},
{"设置英雄点数","Set hero points"},
{"当前法术：请在游戏中选择法术","Spell: select a spell in the game"},
{"法术升级点数（每个法术独立）","Spell points (independent per spell)"},
{"设置法术点数","Set spell points"},
{"兵营士兵数量：各兵营的游戏原值","Barracks: original counts"},
{"统一设置每座兵营的士兵数量（1～50）","Soldiers per barracks (1-50)"},
{"设置兵营数量","Set soldier count"},
{"一键重置兵营","Reset barracks"},
{"影响已建和新建兵营。重置恢复每种兵营各自的原值。","Applies to existing and new barracks. Reset restores each type."},
{"防御塔可用升级点数：","Available tower points: "},
{"防御塔升级点数：请打开升级页面","Tower points: open upgrades"},
{"当前英雄：","Hero: "},
{"  ·  可用点数：","  / Points: "},
{"当前法术：","Spell: "},
{"兵营士兵数量：每座 ","Soldiers per barracks: "},
{" 名",""},
{"Alt+F4 用于关闭窗口，请选择其他快捷键。","Alt+F4 closes windows. Choose another hotkey."},
{"已录入 ","Captured "},
{"，点击“保存快捷键设置”生效。","; click Save hotkeys to apply."},
{"读取快捷键设置失败：","Could not load hotkeys: "},
{"快捷键重复：","Duplicate hotkey: "},
{"。请为每个功能选择不同按键。",". Choose a different key for each action."},
{"快捷键设置已保存；约 0.1 秒后在游戏中生效。","Hotkeys saved; applied in about 0.1 seconds."},
{"保存设置失败：","Could not save settings: "},
{"尚未连接游戏，请使用原来的启动方式打开游戏。","Game not connected. Start the game normally."},
{"正在等待游戏执行上一条修改。","Waiting for the previous command."},
{"正在执行…","Applying..."},
{"发送失败：","Send failed: "},
{"● 已连接游戏","Connected to game"},
{"○ 等待游戏连接","Waiting for game"},
{"游戏连接已断开。重新打开游戏即可连接。","Disconnected. Restart the game to reconnect."},
{"游戏未响应，请检查游戏是否正常运行。","No response. Check that the game is running."},
{"关卡金钱：","Gold: "},
{"解锁星星：","Unlock stars: "},
{"解锁星星：等待选择存档","Unlock stars: select a save"},
{"伤害倍率：","Damage: "},
{" 倍","x"},
{"技能冷却倍率：","Cooldown: "},
{" 倍（","x ("},
{"游戏反馈：","Game response: "},
{"游戏执行超时，请查看游戏窗口。","Command timed out. Check the game window."},
{"修改器已经打开。","The trainer is already running."},
{"）",")"}
  };
  static string L(string text) { string value; return English && EnglishText.TryGetValue(text,out value)?value:text; }
  void TranslateControls(Control parent, bool toEnglish) {
   foreach(Control child in parent.Controls) {
    if(toEnglish) { string value; if(EnglishText.TryGetValue(child.Text,out value))child.Text=value; }
    else foreach(KeyValuePair<string,string> pair in EnglishText)if(pair.Value==child.Text){child.Text=pair.Key;break;}
    TranslateControls(child,toEnglish);
   }
  }

  readonly string root, data;
  readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
  readonly Label connection = new Label(), feedback = new Label(), goldNow = new Label(), starsNow = new Label(), damageNow = new Label(), cooldownNow = new Label();
  readonly NumericUpDown goldInput = new NumericUpDown(), starsInput = new NumericUpDown();
  readonly NumericUpDown towerPointsInput=new NumericUpDown(), heroPointsInput=new NumericUpDown(), powerPointsInput=new NumericUpDown(), barracksInput=new NumericUpDown();
  readonly Label towerPointsNow=new Label(), heroPointsNow=new Label(), powerPointsNow=new Label(), barracksNow=new Label();
  readonly ComboBox towerHistoryBox=new ComboBox();
  string towerHistoryContent="", currentHero="", currentPower="";
  readonly CheckBox[] enabled = new CheckBox[4];
  readonly TextBox[] keyBoxes = new TextBox[4];
  readonly string[] actions = {"add500", "add100", "damage", "cooldown"};
  readonly List<Button> actionButtons = new List<Button>();
  readonly Dictionary<string, string> messages = new Dictionary<string, string> {
   {"ready", "游戏已连接"}, {"gold_updated", "金钱已修改"}, {"damage_updated", "伤害倍率已增加 0.1"},
   {"cooldown_updated", "技能冷却倍率已降低 0.1"}, {"cooldown_limit", "冷却已达到最低 0.1 倍（9 次上限）"},
   {"enter_level_first", "请先进入关卡，再修改金钱"}, {"select_slot_first", "请先在游戏中选择存档"},
   {"stars_saved_reopen_map", "星星已保存；重新进入地图或解锁页面刷新"}, {"invalid_gold", "金钱必须是 0～999999999 的整数"},
   {"invalid_stars", "星星必须是 0～999999 的整数"}, {"save_failed", "存档写入失败，星星修改未保存"},
   {"backup_failed", "存档备份失败，星星修改已取消"},
   {"history_wrong_slot", "这条记录属于其他存档，请先切换到对应存档"},
   {"history_missing", "所选历史记录不存在，请刷新后重试"},
   {"history_write_failed", "无法保存星星历史，本次修改已取消"},
   {"stars_unavailable", "无法读取当前星星数量，请先进入地图"},
   {"tower_points_saved", "防御塔升级点数已保存，修改前的点数已加入历史"},
   {"heroes_points_saved", "当前英雄的升级点数已保存"},
   {"powers_points_saved", "当前法术的升级点数已保存"},
   {"open_tower_points_first", "请在游戏中打开防御塔的全局升级页面"},
   {"open_heroes_points_first", "请在游戏中打开英雄页面并选择英雄"},
   {"open_powers_points_first", "请在游戏中打开法术页面并选择法术"},
   {"unit_changed", "游戏中当前选择已发生变化，请确认名称后重试"},
   {"invalid_points", "升级点数必须是 0～999999 的整数"},
   {"invalid_barracks", "兵营士兵数量必须是 1～50 的整数"},
   {"barracks_updated", "兵营数量已设置；新士兵将按兵营的召唤流程补齐"},
   {"barracks_reset", "兵营数量已重置为各兵营的游戏原值"}
  };
  bool connected, hadConnection;
  string lastAck = "", lastMessage = "", pendingId = "";
  DateTime pendingAt;
  int cooldownSteps;
  string currentSlot = "", historyContent = "";
  readonly ComboBox historyBox = new ComboBox();
  sealed class StarRecord {
   public string Id, Slot, Before, After; public bool Tower;
   public override string ToString() { return L("第 ") + Id + L(" 次编辑前：") + Before + (Tower?L(" 点"):L(" 星")) + L("  ·  存档 ") + Slot; }
  }
  static readonly Color Bg = Color.FromArgb(24, 29, 39), Card = Color.FromArgb(35, 42, 55), Ink = Color.FromArgb(232, 237, 246), Muted = Color.FromArgb(159, 172, 193), Accent = Color.FromArgb(93, 162, 255);

  public Trainer() {
   root = AppDomain.CurrentDomain.BaseDirectory;
   data = Path.Combine(root, "trainer-data"); Directory.CreateDirectory(data);
   Text = L("王国保卫战 6 · 修改器"); ClientSize = new Size(720, 770);
   MinimumSize = MaximumSize = Size; FormBorderStyle = FormBorderStyle.FixedSingle; MaximizeBox = false;
   StartPosition = FormStartPosition.CenterScreen; BackColor = Bg; ForeColor = Ink;
   Font = new Font("Microsoft YaHei UI", 10f); AutoScaleMode = AutoScaleMode.None;
   Label title = LabelAt(L("王国保卫战 6"), 24, 20, 400, 35, Ink, 20); title.Font = new Font(Font.FontFamily, 20, FontStyle.Bold);
   LabelAt(L("实时修改器  /  升级点数、兵营数量与历史恢复"), 26, 58, 550, 24, Muted, 10);
   connection.SetBounds(26, 94, 480, 25); connection.ForeColor = Muted; Controls.Add(connection);
   CheckBox top = new CheckBox { Text = L("窗口置顶"), ForeColor = Muted, BackColor = Bg, AutoSize = true, Location = new Point(590, 94) };
   top.CheckedChanged += delegate { TopMost = top.Checked; }; Controls.Add(top);
   string[] labels = {L("金钱 +500"), L("金钱 +100"), L("防御塔伤害倍率 +0.1"), L("防御塔技能冷却倍率 −0.1")};
   string[] defaults = {"f6", "f7", "f8", "f9"};
   for (int i=0; i<4; i++) {
    int row = 135+i*65; int index = i;
    Panel p = new Panel { BackColor = Card, Location = new Point(24,row), Size = new Size(672,57) }; Controls.Add(p);
    Button b = MakeButton(labels[i], 12, 10, 252, 37); p.Controls.Add(b); b.Click += delegate { Send(actions[index], ""); }; actionButtons.Add(b);
    enabled[i] = new CheckBox { Text = L("启用快捷键"), ForeColor = Ink, BackColor = Card, Location = new Point(285,17), AutoSize = true };
    p.Controls.Add(enabled[i]); enabled[i].CheckedChanged += delegate { keyBoxes[index].Enabled=enabled[index].Checked; };
    keyBoxes[i] = new TextBox { Text=defaults[i], Location=new Point(408,14), Size=new Size(165,29), ReadOnly=true, Enabled=false, BackColor=Bg, ForeColor=Ink, BorderStyle=BorderStyle.FixedSingle, TextAlign=HorizontalAlignment.Center, Tag=defaults[i] };
    keyBoxes[i].KeyDown += delegate(object sender, KeyEventArgs e) { CaptureKey(index,e); }; p.Controls.Add(keyBoxes[i]);
    Label hint = new Label { Text=L("点击后按键"), ForeColor=Muted, BackColor=Card, Location=new Point(580,18), Size=new Size(87,20), Font=new Font(Font.FontFamily,8.5f) }; p.Controls.Add(hint);
   }
   damageNow.SetBounds(28, 402, 275, 26); cooldownNow.SetBounds(312,402,380,26); damageNow.ForeColor=cooldownNow.ForeColor=Accent;
   Controls.Add(damageNow);Controls.Add(cooldownNow);damageNow.Text=L("伤害倍率：等待连接");cooldownNow.Text=L("技能冷却倍率：等待连接");
   Button save = MakeButton(L("保存快捷键设置"), 510, 435, 185, 34); save.Click += delegate { SaveBindings(true); }; Controls.Add(save);
   LabelAt(L("快捷键在游戏窗口中生效；保存后的设置会保留。"), 26,440,476,22,Muted,9);
   starsNow.SetBounds(28, 482, 222, 25); goldNow.SetBounds(28, 525, 222, 25); Controls.Add(starsNow);Controls.Add(goldNow);
   starsNow.Text=L("解锁星星：等待存档");goldNow.Text=L("关卡金钱：等待进入关卡");
   SetupNumber(starsInput, 265,478,999999);SetupNumber(goldInput,265,521,999999999);
   Button starsSet = MakeButton(L("设置星星"), 510, 478, 185, 34), goldSet = MakeButton(L("设置金钱"),510,521,185,34);
   starsSet.Click += delegate { Send("setstars", starsInput.Value.ToString(CultureInfo.InvariantCulture)); };
   goldSet.Click += delegate { Send("setgold", goldInput.Value.ToString(CultureInfo.InvariantCulture)); };
   Controls.Add(starsSet);Controls.Add(goldSet);actionButtons.Add(starsSet);actionButtons.Add(goldSet);
   feedback.SetBounds(26, 708, 665, 42);feedback.ForeColor=Muted;feedback.Text=L("使用原来的启动方式打开游戏，再选择存档或进入关卡。");Controls.Add(feedback);
   LabelAt(L("星星修改历史（按编辑顺序，记录修改前的数量）"), 28, 575, 645, 23, Muted, 10);
   historyBox.SetBounds(27, 609, 462, 34); historyBox.DropDownStyle=ComboBoxStyle.DropDownList;
   historyBox.BackColor=Card;historyBox.ForeColor=Ink; Controls.Add(historyBox);
   Button restore=MakeButton(L("恢复选中记录"),510,605,185,36);
   restore.Click += delegate {
    StarRecord entry=historyBox.SelectedItem as StarRecord;
    if(entry==null){feedback.Text=L("请先选择一条历史记录。");return;}
    if(entry.Slot!=currentSlot){feedback.Text=L(messages["history_wrong_slot"]);return;}
    Send("restorestars",entry.Id);
   };
   Controls.Add(restore);actionButtons.Add(restore);
   TabControl tabs=new TabControl {Location=new Point(16,125),Size=new Size(690,560)};
   TabPage battle=new TabPage(L("战斗与星星")) {BackColor=Bg,ForeColor=Ink};
   TabPage resources=new TabPage(L("升级点数与兵营")) {BackColor=Bg,ForeColor=Ink};
   List<Control> existing=new List<Control>();
   foreach(Control control in Controls)if(control.Top>=130&&control!=feedback)existing.Add(control);
   foreach(Control control in existing){control.Location=new Point(control.Left-16,control.Top-130);battle.Controls.Add(control);}
   tabs.TabPages.Add(battle);tabs.TabPages.Add(resources);Controls.Add(tabs);BuildResourceUI(resources);
   ComboBox language=new ComboBox {Location=new Point(580,55),Size=new Size(116,26),DropDownStyle=ComboBoxStyle.DropDownList};
   language.Items.AddRange(new object[]{"中文", "English"});language.SelectedIndex=English?1:0;Controls.Add(language);
   language.SelectedIndexChanged+=delegate {
    bool next=language.SelectedIndex==1;if(next==English)return;
    TranslateControls(this,next);English=next;Text=L("王国保卫战 6 · 修改器");
    historyContent="";towerHistoryContent="";lastMessage="";RefreshHistory();RefreshTowerHistory();Poll();
    try{File.WriteAllText(Path.Combine(data,"language.txt"),English?"en":"zh");}catch(Exception e){feedback.Text=L("保存设置失败：")+e.Message;}
   };
   LoadBindings();
   timer.Interval=250;timer.Tick += delegate { Poll(); }; timer.Start(); Poll();
  }
  void BuildResourceUI(TabPage page) {
   towerPointsNow.SetBounds(12,20,365,32);towerPointsNow.Text=L("防御塔升级点数：等待升级页面");page.Controls.Add(towerPointsNow);
   SetupNumber(towerPointsInput,389,17,999999);towerPointsInput.Width=119;page.Controls.Add(towerPointsInput);
   Button towerSet=MakeButton(L("设置塔升级点数"),522,14,144,37);towerSet.Click+=delegate {Send("settowerpoints",towerPointsInput.Value.ToString(CultureInfo.InvariantCulture));};page.Controls.Add(towerSet);actionButtons.Add(towerSet);
   Label h=new Label {Text=L("防御塔升级点数历史（记录每次修改前的可用点数）"),ForeColor=Muted,Location=new Point(12,65),Size=new Size(650,24)};page.Controls.Add(h);
   towerHistoryBox.SetBounds(12,95,496,33);towerHistoryBox.DropDownStyle=ComboBoxStyle.DropDownList;page.Controls.Add(towerHistoryBox);
   Button restore=MakeButton(L("恢复选中点数"),522,91,144,37);restore.Click+=delegate {
    StarRecord record=towerHistoryBox.SelectedItem as StarRecord;
    if(record==null){feedback.Text=L("请先选择一条升级点数历史记录。");return;}
    if(record.Slot!=currentSlot){feedback.Text=L(messages["history_wrong_slot"]);return;}
    Send("restoretowerpoints",record.Id);
   };page.Controls.Add(restore);actionButtons.Add(restore);
   heroPointsNow.SetBounds(12,157,650,42);heroPointsNow.Text=L("当前英雄：请在游戏中选择英雄");heroPointsNow.ForeColor=Accent;page.Controls.Add(heroPointsNow);
   Label heroHint=new Label {Text=L("英雄升级点数（每名英雄独立）"),ForeColor=Muted,Location=new Point(12,211),Size=new Size(367,24)};page.Controls.Add(heroHint);
   SetupNumber(heroPointsInput,389,205,999999);heroPointsInput.Width=119;page.Controls.Add(heroPointsInput);
   Button heroSet=MakeButton(L("设置英雄点数"),522,201,144,37);heroSet.Click+=delegate {
    if(currentHero.Length==0){feedback.Text=L(messages["open_heroes_points_first"]);return;}
    Send("setheropoints",currentHero+","+heroPointsInput.Value.ToString(CultureInfo.InvariantCulture));
   };page.Controls.Add(heroSet);actionButtons.Add(heroSet);
   powerPointsNow.SetBounds(12,269,650,42);powerPointsNow.Text=L("当前法术：请在游戏中选择法术");powerPointsNow.ForeColor=Accent;page.Controls.Add(powerPointsNow);
   Label powerHint=new Label {Text=L("法术升级点数（每个法术独立）"),ForeColor=Muted,Location=new Point(12,323),Size=new Size(367,24)};page.Controls.Add(powerHint);
   SetupNumber(powerPointsInput,389,317,999999);powerPointsInput.Width=119;page.Controls.Add(powerPointsInput);
   Button powerSet=MakeButton(L("设置法术点数"),522,313,144,37);powerSet.Click+=delegate {
    if(currentPower.Length==0){feedback.Text=L(messages["open_powers_points_first"]);return;}
    Send("setpowerpoints",currentPower+","+powerPointsInput.Value.ToString(CultureInfo.InvariantCulture));
   };page.Controls.Add(powerSet);actionButtons.Add(powerSet);
   barracksNow.SetBounds(12,381,650,32);barracksNow.Text=L("兵营士兵数量：各兵营的游戏原值");page.Controls.Add(barracksNow);
   Label countHint=new Label {Text=L("统一设置每座兵营的士兵数量（1～50）"),ForeColor=Muted,Location=new Point(12,429),Size=new Size(367,24)};page.Controls.Add(countHint);
   SetupNumber(barracksInput,389,423,50);barracksInput.Minimum=1;barracksInput.Value=5;barracksInput.Width=119;page.Controls.Add(barracksInput);
   Button countSet=MakeButton(L("设置兵营数量"),522,419,144,37);countSet.Click+=delegate {Send("setbarracks",barracksInput.Value.ToString(CultureInfo.InvariantCulture));};page.Controls.Add(countSet);actionButtons.Add(countSet);
   Button reset=MakeButton(L("一键重置兵营"),522,476,144,37);reset.Click+=delegate {Send("resetbarracks","");};page.Controls.Add(reset);actionButtons.Add(reset);
   Label help=new Label {Text=L("影响已建和新建兵营。重置恢复每种兵营各自的原值。"),ForeColor=Muted,Location=new Point(12,480),Size=new Size(495,36),Font=new Font(Font.FontFamily,9f)};page.Controls.Add(help);
  }
  void UpdateResourceState(Dictionary<string,string> state) {
   string value;
   if(state.TryGetValue("tower_points",out value))towerPointsNow.Text=value.Length>0?L("防御塔可用升级点数：")+value:L("防御塔升级点数：请打开升级页面");
   string hero=state.ContainsKey("heroes_id")?state["heroes_id"]:"";
   string power=state.ContainsKey("powers_id")?state["powers_id"]:"";
   if(hero!=currentHero){currentHero=hero;decimal n;if(state.ContainsKey("heroes_points")&&decimal.TryParse(state["heroes_points"],out n))heroPointsInput.Value=Math.Max(0,Math.Min(heroPointsInput.Maximum,n));}
   if(power!=currentPower){currentPower=power;decimal n;if(state.ContainsKey("powers_points")&&decimal.TryParse(state["powers_points"],out n))powerPointsInput.Value=Math.Max(0,Math.Min(powerPointsInput.Maximum,n));}
   heroPointsNow.Text=hero.Length>0?L("当前英雄：")+(state.ContainsKey("heroes_name")?state["heroes_name"]:hero)+L("  ·  可用点数：")+state["heroes_points"]:L("当前英雄：请在游戏中选择英雄");
   powerPointsNow.Text=power.Length>0?L("当前法术：")+(state.ContainsKey("powers_name")?state["powers_name"]:power)+L("  ·  可用点数：")+state["powers_points"]:L("当前法术：请在游戏中选择法术");
   if(state.TryGetValue("barracks_count",out value))barracksNow.Text=value.Length>0?L("兵营士兵数量：每座 ")+value+L(" 名"):L("兵营士兵数量：各兵营的游戏原值");
   RefreshTowerHistory();
  }
  void RefreshTowerHistory() {
   try {
    string path=Path.Combine(data,"tower-points-history.tsv");if(!File.Exists(path))return;
    string text;using(FileStream fs=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete))using(StreamReader reader=new StreamReader(fs,Encoding.UTF8))text=reader.ReadToEnd();
    if(text==towerHistoryContent)return;
    StarRecord previous=towerHistoryBox.SelectedItem as StarRecord;string selected=previous==null?"":previous.Id;
    towerHistoryBox.BeginUpdate();towerHistoryBox.Items.Clear();
    foreach(string line in text.Split(new char[]{'\n','\r'},StringSplitOptions.RemoveEmptyEntries)) {
     string[] f=line.Split('\t');if(f.Length!=4||(currentSlot.Length>0&&f[1]!=currentSlot))continue;
     StarRecord entry=new StarRecord {Id=f[0],Slot=f[1],Before=f[2],After=f[3],Tower=true};int index=towerHistoryBox.Items.Add(entry);if(entry.Id==selected)towerHistoryBox.SelectedIndex=index;
    }
    if(towerHistoryBox.SelectedIndex<0&&towerHistoryBox.Items.Count>0)towerHistoryBox.SelectedIndex=towerHistoryBox.Items.Count-1;
    towerHistoryBox.EndUpdate();towerHistoryContent=text;
   } catch { }
  }
  Label LabelAt(string text,int x,int y,int w,int h,Color color,float size) {
   Label l=new Label {Text=text,ForeColor=color,Location=new Point(x,y),Size=new Size(w,h),Font=new Font(Font.FontFamily,size)};Controls.Add(l);return l;
  }
  Button MakeButton(string text,int x,int y,int w,int h) {
   Button b=new Button {Text=text,Location=new Point(x,y),Size=new Size(w,h),FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(48,65,88),ForeColor=Ink,Cursor=Cursors.Hand};
   b.FlatAppearance.BorderColor=Color.FromArgb(62,87,117);b.FlatAppearance.MouseOverBackColor=Color.FromArgb(62,86,119);return b;
  }
  void SetupNumber(NumericUpDown n,int x,int y,decimal max) {
   n.SetBounds(x,y,220,34);n.Minimum=0;n.Maximum=max;n.ThousandsSeparator=true;n.BackColor=Card;n.ForeColor=Ink;Controls.Add(n);
  }
  public static string KeyName(Keys k) {
   if(k>=Keys.F1 && k<=Keys.F24)return "f"+((int)k-(int)Keys.F1+1);
   if(k>=Keys.A && k<=Keys.Z)return k.ToString().ToLowerInvariant();
   if(k>=Keys.D0 && k<=Keys.D9)return ((int)k-(int)Keys.D0).ToString();
   if(k>=Keys.NumPad0 && k<=Keys.NumPad9)return "kp"+((int)k-(int)Keys.NumPad0);
   switch(k) {
    case Keys.Space:return "space"; case Keys.Enter:return "return";case Keys.Tab:return "tab";
    case Keys.Insert:return "insert";case Keys.Delete:return "delete";case Keys.Home:return "home";case Keys.End:return "end";
    case Keys.PageUp:return "pageup";case Keys.PageDown:return "pagedown";case Keys.Up:return "up";case Keys.Down:return "down";
    case Keys.Left:return "left";case Keys.Right:return "right";case Keys.Back:return "backspace";case Keys.Escape:return "escape";
    case Keys.OemMinus:return "-";case Keys.Oemplus:return "=";case Keys.Oemcomma:return ",";case Keys.OemPeriod:return ".";
    case Keys.OemOpenBrackets:return "[";case Keys.OemCloseBrackets:return "]";case Keys.OemQuestion:return "/";
    case Keys.OemSemicolon:return ";";case Keys.OemQuotes:return "'";case Keys.OemBackslash:return "\\";
    case Keys.Add:return "kp+";case Keys.Subtract:return "kp-";case Keys.Multiply:return "kp*";case Keys.Divide:return "kp/";
   }
   return null;
  }
  void CaptureKey(int i, KeyEventArgs e) {
   e.Handled=true;e.SuppressKeyPress=true;
   string key=KeyName(e.KeyCode);if(key==null)return;
   if(e.Alt && e.KeyCode==Keys.F4){feedback.Text=L("Alt+F4 用于关闭窗口，请选择其他快捷键。");return;}
   string combo=(e.Control?"ctrl+":"")+(e.Alt?"alt+":"")+(e.Shift?"shift+":"")+key;
   keyBoxes[i].Tag=combo;keyBoxes[i].Text=combo;feedback.Text=L("已录入 ")+combo+L("，点击“保存快捷键设置”生效。");
  }
  void LoadBindings() {
   
   try {
    string p=Path.Combine(data,"settings.ini");if(!File.Exists(p)){SaveBindings(false);return;}
    Dictionary<string,string> kv=ReadPairs(p,'=');
    for(int i=0;i<4;i++) {
     string v;
     if(kv.TryGetValue("key_"+actions[i],out v)&&v.Length>0){keyBoxes[i].Tag=v;keyBoxes[i].Text=v;}
     if(kv.TryGetValue("hotkey_"+actions[i],out v)&&v.Length>0){enabled[i].Checked=true;keyBoxes[i].Tag=v;keyBoxes[i].Text=v;}
    }
   } catch(Exception e){feedback.Text=L("读取快捷键设置失败：")+e.Message;}
   
  }
  void SaveBindings(bool show) {
   HashSet<string> used=new HashSet<string>();StringBuilder s=new StringBuilder();
   for(int i=0;i<4;i++) {
    string key=(string)keyBoxes[i].Tag;
    if(enabled[i].Checked&&!used.Add(key)){feedback.Text=L("快捷键重复：")+key+L("。请为每个功能选择不同按键。");return;}
    s.Append("key_").Append(actions[i]).Append('=').Append(key).Append('\n');
    s.Append("hotkey_").Append(actions[i]).Append('=').Append(enabled[i].Checked?key:"").Append('\n');
   }
   try {
    string path=Path.Combine(data,"settings.ini"),tmp=path+".tmp";
    File.WriteAllText(tmp,s.ToString(),new UTF8Encoding(false));
    if(File.Exists(path))File.Replace(tmp,path,null);else File.Move(tmp,path);
    if(show)feedback.Text=L("快捷键设置已保存；约 0.1 秒后在游戏中生效。");
   } catch(Exception e){feedback.Text=L("保存设置失败：")+e.Message;}
  }
  static Dictionary<string,string> ReadPairs(string path,char separator) {
   Dictionary<string,string> result=new Dictionary<string,string>();
   using(FileStream fs=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete))
   using(StreamReader r=new StreamReader(fs,Encoding.UTF8)) {
    string line;while((line=r.ReadLine())!=null){int p=line.IndexOf(separator);if(p>0)result[line.Substring(0,p)]=line.Substring(p+1);}
   }
   return result;
  }
  void Send(string action,string value) {
   if(!connected){feedback.Text=L("尚未连接游戏，请使用原来的启动方式打开游戏。");return;}
   if(pendingId.Length>0){feedback.Text=L("正在等待游戏执行上一条修改。");return;}
   if(action=="cooldown" && cooldownSteps>=9){feedback.Text=L(messages["cooldown_limit"]);return;}
   try {
    string id=Guid.NewGuid().ToString("N");
    using(FileStream fs=new FileStream(Path.Combine(data,"commands.tsv"),FileMode.Append,FileAccess.Write,FileShare.ReadWrite)) {
     byte[] bytes=Encoding.UTF8.GetBytes(id+"\t"+action+"\t"+value+"\n");fs.Write(bytes,0,bytes.Length);fs.Flush(true);
    }
    pendingId=id;pendingAt=DateTime.UtcNow;feedback.Text=L("正在执行…");
   } catch(Exception e){feedback.Text=L("发送失败：")+e.Message;}
  }
  void Poll() {
   string path=Path.Combine(data,"status.tsv");Dictionary<string,string> state=null;
   try {
    connected=File.Exists(path)&&(DateTime.UtcNow-File.GetLastWriteTimeUtc(path)).TotalSeconds<3;
    if(connected){state=ReadPairs(path,'\t');connected=state.ContainsKey("protocol")&&state["protocol"]=="1";}
   } catch {connected=false;}
   connection.Text=connected?L("● 已连接游戏"):L("○ 等待游戏连接");connection.ForeColor=connected?Color.FromArgb(118,215,161):Muted;
   foreach(Button b in actionButtons)b.Enabled=connected;
   if(!connected) {
    if(hadConnection){feedback.Text=L("游戏连接已断开。重新打开游戏即可连接。");hadConnection=false;}
    if(pendingId.Length>0&&(DateTime.UtcNow-pendingAt).TotalSeconds>5){pendingId="";feedback.Text=L("游戏未响应，请检查游戏是否正常运行。");}
    return;
   }
   hadConnection=true;
   string slotId;
   if(state.TryGetValue("slot",out slotId) && slotId!=currentSlot) {currentSlot=slotId;historyContent="";towerHistoryContent="";currentHero="";currentPower="";}
   RefreshHistory();
   UpdateResourceState(state);
   string v;
   if(state.TryGetValue("gold",out v))goldNow.Text=v.Length>0?L("关卡金钱：")+v:L("关卡金钱：等待进入关卡");
   if(state.TryGetValue("stars",out v))starsNow.Text=v.Length>0?L("解锁星星：")+v:L("解锁星星：等待选择存档");
   if(state.TryGetValue("damage",out v))damageNow.Text=L("伤害倍率：")+v+L(" 倍");
   int.TryParse(state.ContainsKey("cooldown_steps")?state["cooldown_steps"]:"0",out cooldownSteps);
   if(state.TryGetValue("cooldown",out v))cooldownNow.Text=L("技能冷却倍率：")+v+L(" 倍（")+cooldownSteps+"/9"+L("）");
   bool acknowledged=false;
   if(state.TryGetValue("ack",out v)&&v.Length>0&&v!=lastAck){lastAck=v;acknowledged=true;if(v==pendingId)pendingId="";}
   if(state.TryGetValue("message",out v)&&(v!=lastMessage||acknowledged)) {
    lastMessage=v;string human;feedback.Text=messages.TryGetValue(v,out human)?L(human):L("游戏反馈：")+v;
   }
   if(pendingId.Length>0&&(DateTime.UtcNow-pendingAt).TotalSeconds>5){pendingId="";feedback.Text=L("游戏执行超时，请查看游戏窗口。");}
  }
  void RefreshHistory() {
   try {
    string path=Path.Combine(data,"stars-history.tsv");if(!File.Exists(path))return;
    string text;
    using(FileStream fs=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete))
    using(StreamReader reader=new StreamReader(fs,Encoding.UTF8))text=reader.ReadToEnd();
    if(text==historyContent)return;
    string selected=(historyBox.SelectedItem as StarRecord)!=null?((StarRecord)historyBox.SelectedItem).Id:"";
    historyBox.BeginUpdate();historyBox.Items.Clear();
    foreach(string line in text.Split(new char[]{'\n','\r'},StringSplitOptions.RemoveEmptyEntries)) {
     string[] fields=line.Split('\t');if(fields.Length!=4)continue;
     if(currentSlot.Length>0 && fields[1]!=currentSlot)continue;
     StarRecord record=new StarRecord {Id=fields[0],Slot=fields[1],Before=fields[2],After=fields[3]};
     int index=historyBox.Items.Add(record);if(record.Id==selected)historyBox.SelectedIndex=index;
    }
    if(historyBox.SelectedIndex<0&&historyBox.Items.Count>0)historyBox.SelectedIndex=historyBox.Items.Count-1;
    historyBox.EndUpdate();historyContent=text;
   } catch { /* Retry an in-progress journal write on the next poll. */ }
  }
  [STAThread] public static void Main(string[] args) {
   English=CultureInfo.CurrentUICulture.TwoLetterISOLanguageName!="zh";
   try{string pref=File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"trainer-data","language.txt")).Trim();if(pref=="en"||pref=="zh")English=pref=="en";}catch{}
   foreach(string arg in args){if(arg=="--en")English=true;if(arg=="--zh")English=false;}
   if(args.Length>0&&args[0]=="--self-test") {
    if(KeyName(Keys.F6)!="f6"||KeyName(Keys.D5)!="5"||KeyName(Keys.NumPad2)!="kp2")Environment.Exit(1);
    English=true;
    if(L("设置英雄点数")!="Set hero points"||L("当前法术的升级点数已保存")!="Selected spell's points saved")Environment.Exit(2);
    English=false;
    if(L("设置英雄点数")!="设置英雄点数")Environment.Exit(3);
    Environment.Exit(0);return;
   }
   bool created;using(Mutex mutex=new Mutex(true,"Local\\KR6GenesisLocalTrainer",out created)) {
    if(!created){MessageBox.Show(L("修改器已经打开。"), L("王国保卫战 6"));return;}
    Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
    if(args.Length>1&&args[0]=="--render") {
     using(Trainer form=new Trainer()) {
      form.ShowInTaskbar=false;form.Opacity=0;form.Show();
      if(args.Length>2&&args[2]=="resources")foreach(Control control in form.Controls)if(control is TabControl)((TabControl)control).SelectedIndex=1;
      Application.DoEvents();
      using(Bitmap bitmap=new Bitmap(form.Width,form.Height)) {
       form.DrawToBitmap(bitmap,new Rectangle(0,0,bitmap.Width,bitmap.Height));bitmap.Save(args[1],System.Drawing.Imaging.ImageFormat.Png);
      }
      form.Close();
     }
     return;
    }
    Application.Run(new Trainer());
   }
  }
 }
}
