using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Windows.Forms;
using System.Collections.Generic;

namespace DesktopPet
{
    public class PetForm : Form
    {
        private int intimacy = 0;
        private string petName = "小宠物";
        private string savePath = Path.Combine(Application.StartupPath, "pet_save.txt");
        private string nameSavePath = Path.Combine(Application.StartupPath, "pet_name.txt");
        private string handCdPath = Path.Combine(Application.StartupPath, "pet_hand_cd.txt");
        private string feedCdPath = Path.Combine(Application.StartupPath, "pet_feed_cd.txt");
        private string statsPath = Path.Combine(Application.StartupPath, "pet_stats.txt");
        private string achievementsPath = Path.Combine(Application.StartupPath, "pet_achievements.txt");
        private string questsPath = Path.Combine(Application.StartupPath, "pet_quests.txt");
        private string crystalsPath = Path.Combine(Application.StartupPath, "pet_crystals.txt");
        private DateTime lastHandshake = DateTime.MinValue;
        private DateTime lastFeed = DateTime.MinValue;
        private DateTime patLastTime = DateTime.MinValue;
        private const int HandCdHours = 1;
        private const int FeedCdHours = 4;
        private const int PatCdSeconds = 30;
        private const int MaxIntimacy = 999;
        private const string CurrentVersion = "1.4.3";
        private const string VersionUrl = "https://cdn.jsdelivr.net/gh/zhzzzzz1/DeskPet@main/version.txt";
        private const string DownloadUrl = "https://cdn.jsdelivr.net/gh/zhzzzzz1/DeskPet@main/DeskPet.exe";
        private Point dragStart;
        private bool isDragging;
        private bool wasOnPet = false;
        private DateTime hoverEnterTime = DateTime.MinValue;
        private Timer reactionTimer;
        private Timer idleTimer;
        private Timer hoverTimer;
        private Timer chatTimer;
        private Timer moodTimer;
        private Timer greetingTimer;
        private HoverMenuForm hoverMenu;
        private Form activeBubble;
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private ContextMenuStrip petMenu;
        private ToolTip petToolTip;
        private Icon appIcon;
        private PetDetailsForm detailsForm;
        private StatsForm statsForm;
        private AchievementForm achievementForm;
        private QuestForm questForm;
        private PanelBubble panelBubble;

        // 任务系统
        private int crystals = 0;
        private string questCompletedDate = "";
        private Dictionary<string, int> questProgress = new Dictionary<string, int>();
        private Dictionary<string, bool> questClaimed = new Dictionary<string, bool>();
        public struct QuestDef { public string id; public string name; public string desc; public int target; public int reward; public string icon; }
        private QuestDef[] questDefs = new QuestDef[]
        {
            new QuestDef{ id="daily_login", name="每日签到", desc="今日首次登录", target=1, reward=10, icon="📅" },
            new QuestDef{ id="daily_interact", name="活跃互动", desc="完成握手/喂食/摸头共3次", target=3, reward=15, icon="🎯" },
            new QuestDef{ id="daily_handshake", name="握个手吧", desc="握手1次", target=1, reward=5, icon="🤝" },
            new QuestDef{ id="daily_feed", name="喂食时间", desc="喂食1次", target=1, reward=8, icon="🍖" },
            new QuestDef{ id="daily_pat", name="摸摸头", desc="摸头3次", target=3, reward=5, icon="✋" },
        };

        // 里程碑成就任务
        private string milestoneQuestsPath = Path.Combine(Application.StartupPath, "pet_milestone_quests.txt");
        private Dictionary<string, int> milestoneProgress = new Dictionary<string, int>();
        private Dictionary<string, bool> milestoneClaimed = new Dictionary<string, bool>();
        private QuestDef[] milestoneQuestDefs = new QuestDef[]
        {
            new QuestDef{ id="ms_pat_50", name="摸头新手", desc="累计摸头50次", target=50, reward=20, icon="✋" },
            new QuestDef{ id="ms_pat_100", name="摸头熟手", desc="累计摸头100次", target=100, reward=30, icon="✋" },
            new QuestDef{ id="ms_pat_200", name="摸头达人", desc="累计摸头200次", target=200, reward=50, icon="✋" },
            new QuestDef{ id="ms_pat_400", name="摸头大师", desc="累计摸头400次", target=400, reward=80, icon="✋" },
            new QuestDef{ id="ms_hand_50", name="握手新手", desc="累计握手50次", target=50, reward=20, icon="🤝" },
            new QuestDef{ id="ms_hand_100", name="握手熟手", desc="累计握手100次", target=100, reward=30, icon="🤝" },
            new QuestDef{ id="ms_hand_200", name="握手达人", desc="累计握手200次", target=200, reward=50, icon="🤝" },
            new QuestDef{ id="ms_hand_400", name="握手大师", desc="累计握手400次", target=400, reward=80, icon="🤝" },
            new QuestDef{ id="ms_feed_50", name="喂食新手", desc="累计喂食50次", target=50, reward=20, icon="🍖" },
            new QuestDef{ id="ms_feed_100", name="喂食熟手", desc="累计喂食100次", target=100, reward=30, icon="🍖" },
            new QuestDef{ id="ms_feed_200", name="喂食达人", desc="累计喂食200次", target=200, reward=50, icon="🍖" },
            new QuestDef{ id="ms_feed_400", name="喂食大师", desc="累计喂食400次", target=400, reward=80, icon="🍖" },
            new QuestDef{ id="ms_days_7", name="初来乍到", desc="累计登录7天", target=7, reward=30, icon="📅" },
            new QuestDef{ id="ms_days_30", name="忠实伙伴", desc="累计登录30天", target=30, reward=50, icon="📅" },
            new QuestDef{ id="ms_days_90", name="长久陪伴", desc="累计登录90天", target=90, reward=80, icon="📅" },
            new QuestDef{ id="ms_days_180", name="不离不弃", desc="累计登录180天", target=180, reward=120, icon="📅" },
            new QuestDef{ id="ms_days_365", name="一年之约", desc="累计登录365天", target=365, reward=200, icon="📅" },
        };

        // 现有反应语句
        private string[] handReactions = { "你好呀~ ", "嘿嘿~", "握手开心!", "软乎乎的~" };
        private string[] feedReactions = { "好吃!", "再来一点~", "好饱呀", "谢谢~" };
        private string[] patReactions = { "好舒服~", "咕噜噜~", "再摸摸~", "开心!" };

        // 【新增】心情系统
        private enum PetMood { Happy, Normal, Sleepy, Lonely, Excited }
        private PetMood currentMood = PetMood.Normal;
        private string[] moodEmojis = { "(◕‿◕)", "(•‿•)", "(～￣▽￣)～", "(︶︹︺)", "(✧ω✧)" };
        private string[] moodDescriptions = { "开心", "普通", "困困的", "孤单", "兴奋" };

        // 【新增】闲聊气泡内容
        private string[] chatMessages = {
            "今天天气真好呢~",
            "你在忙什么呀？",
            "我想出去玩...",
            "给我点好吃的嘛~",
            "你最好啦！(≧∇≦)/",
            "哼，不理你了...才怪！",
            "今天也要加油哦！",
            "我好喜欢你呀~",
            "要不要一起玩游戏？",
            "时间过得好快呀...",
            "你在想什么呢？",
            "我刚刚做了一个梦哦~",
            "能不能摸摸我？",
            "嘿嘿，被你发现了~",
            "今天也是元气满满的一天！",
            "好无聊啊...陪我玩嘛~",
            "你是最棒的！(๑•̀ㅂ•́)و✧",
            "我饿了...有吃的吗？",
            "窗外风景不错耶~",
            "爱你哟~ ❤️"
        };

        // 【新增】时间问候语
        private string[] morningGreetings = { "早安呀~ 新的一天开始啦！", "早上好！今天也要元气满满哦~", "早安！你醒了吗？我等你好久啦~" };
        private string[] noonGreetings = { "午安~ 忙了一上午累不累？", "中午好！该休息一下啦~", "下午好呀！吃午饭了吗？" };
        private string[] eveningGreetings = { "晚上好~ 今天辛苦了！", "傍晚了，要不要放松一下？", "晚安快要到咯~" };
        private string[] nightGreetings = { "夜深了，早点休息哦...", "晚安~ 做个好梦！", "这么晚还在吗？要注意身体呀..." };
        private bool greetedToday = false;
        private int lastGreetingHour = -1;

        // 【新增】成就系统
        private HashSet<string> unlockedAchievements = new HashSet<string>();
        public struct AchievementDef { public string id; public string name; public string desc; public int target; public string icon; }
        private AchievementDef[] allAchievements = new AchievementDef[]
        {
            new AchievementDef{ id="first_handshake", name="初次握手", desc="第一次和宠物握手", target=1, icon="🤝" },
            new AchievementDef{ id="first_feed", name="第一顿饭", desc="第一次喂食宠物", target=1, icon="🍖" },
            new AchievementDef{ id="first_pat", name="温柔的抚摸", desc="第一次摸头", target=1, icon="👋" },
            new AchievementDef{ id="handshake10", name="握手达人", desc="累计握手10次", target=10, icon="🤝" },
            new AchievementDef{ id="feed10", name="喂养专家", desc="累计喂食10次", target=10, icon="🍽️" },
            new AchievementDef{ id="pat50", name="摸头狂魔", desc="累计摸头50次", target=50, icon="✋" },
            new AchievementDef{ id="intimacy20", name="初识好友", desc="亲密度达到20", target=20, icon="⭐" },
            new AchievementDef{ id="intimacy50", name="莫逆之交", desc="亲密度达到50", target=50, icon="🌟" },
            new AchievementDef{ id="intimacy100", name="灵魂伴侣", desc="亲密度达到100", target=100, icon="💫" },
            new AchievementDef{ id="intimacy999", name="满级挚爱", desc="亲密度达到999", target=999, icon="💖" },
            new AchievementDef{ id="level4", name="最高等级", desc="等级达到Lv.4", target=1, icon="👑" },
            new AchievementDef{ id="rename", name="取名大师", desc="给宠物改过名字", target=1, icon="✏️" },
            new AchievementDef{ id="night_owl", name="夜猫子", desc="在23点后还和宠物互动", target=1, icon="🦉" },
            new AchievementDef{ id="early_bird", name="早起鸟", desc="在7点前和宠物互动", target=1, icon="🐦" },
        };

        // 【新增】互动统计
        private int totalHandshakes = 0;
        private int totalFeeds = 0;
        private int totalPats = 0;
        private DateTime firstInteractionDate = DateTime.MinValue;
        private DateTime lastInteractionDate = DateTime.Now;

        // 【新增】皮肤/外观系统
        private int currentSkinIndex = 0;
        private string[] skinNames = { "默认皮肤", "暖色系", "冷色系", "梦幻系", "暗夜系" };
        private Color[][] skinColors = new Color[][]
        {
            new Color[]{ Color.FromArgb(255,140,100), Color.FromArgb(100,185,130), Color.FromArgb(255,240,232) },
            new Color[]{ Color.FromArgb(230,120,80), Color.FromArgb(200,160,60), Color.FromArgb(255,245,235) },
            new Color[]{ Color.FromArgb(80,140,200), Color.FromArgb(80,180,160), Color.FromArgb(235,245,255) },
            new Color[]{ Color.FromArgb(200,130,220), Color.FromArgb(150,200,180), Color.FromArgb(250,240,255) },
            new Color[]{ Color.FromArgb(100,100,140), Color.FromArgb(80,130,120), Color.FromArgb(40,42,54) },
        };

        // 【新增】昼夜模式
        private bool isNightMode = false;

        private static readonly Color TranspColor = Color.Fuchsia;
        private const int BlackThreshold = 60;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x00000080;
                return cp;
            }
        }

        public PetForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost = true;
            this.AllowTransparency = true;
            this.TransparencyKey = TranspColor;
            this.BackColor = TranspColor;
            this.DoubleBuffered = true;
            this.StartPosition = FormStartPosition.Manual;
            this.ClientSize = new Size(128, 128);
            this.Text = "DeskPet";
            this.ShowInTaskbar = false;
            this.Cursor = Cursors.Hand;

            int screenW = Screen.PrimaryScreen.WorkingArea.Width;
            int screenH = Screen.PrimaryScreen.WorkingArea.Height;
            this.Location = new Point(screenW - 148, screenH - 170);

            string imagePath = Path.Combine(Application.StartupPath, "fa95e2196cc26bf8fc14185267fadb22.jpg");
            if (File.Exists(imagePath))
            {
                this.BackgroundImage = RemoveBlackBackground(imagePath);
                this.BackgroundImageLayout = ImageLayout.Zoom;
            }

            string iconPath = Path.Combine(Application.StartupPath, "微信图片_20260711143702_6_2.jpg");
            if (File.Exists(iconPath))
            {
                Bitmap iconBmp = new Bitmap(iconPath);
                IntPtr hIcon = iconBmp.GetHicon();
                appIcon = Icon.FromHandle(hIcon);
                this.Icon = appIcon;
            }

            petToolTip = new ToolTip();
            petToolTip.SetToolTip(this, petName + " | 亲密度: 0");

            petMenu = new ContextMenuStrip();
            petMenu.Items.Add("宠物面板", null, OpenDetails);
            petMenu.Items.Add("互动统计", null, OpenStats);
            petMenu.Items.Add("成就墙", null, OpenAchievements);
            petMenu.Items.Add("-", null, null);
            ToolStripMenuItem skinItem = new ToolStripMenuItem("切换皮肤");
            for (int i = 0; i < skinNames.Length; i++)
            {
                int idx = i;
                ToolStripMenuItem si = new ToolStripMenuItem(skinNames[i], null, (s, ev) => SelectSkin(idx));
                si.Checked = (idx == currentSkinIndex);
                skinItem.DropDownItems.Add(si);
            }
            petMenu.Items.Add(skinItem);
            petMenu.Items.Add("-", null, null);
            petMenu.Items.Add("任务系统", null, OpenQuests);
            petMenu.Items.Add("-", null, null);
            petMenu.Items.Add("退出", null, ExitPet);
            this.ContextMenuStrip = petMenu;

            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("显示", null, ShowPet);
            trayMenu.Items.Add("复位", null, ResetPosition);
            trayMenu.Items.Add("宠物面板", null, OpenDetails);
            trayMenu.Items.Add("-", null, null);
            trayMenu.Items.Add("互动统计", null, OpenStats);
            trayMenu.Items.Add("-", null, null);
            trayMenu.Items.Add("任务系统", null, OpenQuests);
            trayMenu.Items.Add("-", null, null);
            trayMenu.Items.Add("退出", null, ExitPet);
            trayIcon = new NotifyIcon();
            trayIcon.Text = "桌面宠物";
            trayIcon.Icon = appIcon ?? SystemIcons.Application;
            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.DoubleClick += ShowPet;
            trayIcon.Visible = true;

            this.MouseDown += OnMouseDown;
            this.MouseMove += OnMouseMove;
            this.MouseUp += (s, e) => { isDragging = false; };
            this.DoubleClick += OnDoubleClickPet;

            reactionTimer = new Timer();
            reactionTimer.Interval = 2500;
            reactionTimer.Tick += (s, e) =>
            {
                petToolTip.SetToolTip(this, petName + " | " + GetIdleMessage());
                reactionTimer.Stop();
            };

            hoverTimer = new Timer();
            hoverTimer.Interval = 200;
            hoverTimer.Enabled = true;
            hoverTimer.Tick += HoverTimer_Tick;

            this.MouseEnter += (s, e) => { wasOnPet = false; hoverEnterTime = DateTime.MinValue; };

            idleTimer = new Timer();
            idleTimer.Interval = 20000;
            idleTimer.Tick += IdleTimer_Tick;
            idleTimer.Start();

            // 闲聊计时器
            chatTimer = new Timer();
            chatTimer.Interval = 45000;
            chatTimer.Tick += ChatTimer_Tick;
            chatTimer.Start();

            // 心情更新计时器
            moodTimer = new Timer();
            moodTimer.Interval = 300000;
            moodTimer.Tick += MoodTimer_Tick;
            moodTimer.Start();

            // 问候检测计时器
            greetingTimer = new Timer();
            greetingTimer.Interval = 60000;
            greetingTimer.Tick += GreetingTimer_Tick;
            greetingTimer.Start();

            LoadData();
            this.FormClosing += OnFormClosing;
            UpdateDayNightMode();
            ShowStartupBubble();
        }

        // ==================== 心情系统 ====================
        private void UpdateMood()
        {
            int hour = DateTime.Now.Hour;
            Random rnd = new Random();
            if (hour >= 0 && hour < 6)
                currentMood = PetMood.Sleepy;
            else if (hour >= 6 && hour < 9)
                currentMood = intimacy >= 50 ? PetMood.Excited : PetMood.Happy;
            else if (hour >= 9 && hour < 12)
                currentMood = PetMood.Happy;
            else if (hour >= 12 && hour < 14)
                currentMood = rnd.Next(2) == 0 ? PetMood.Sleepy : PetMood.Normal;
            else if (hour >= 14 && hour < 18)
                currentMood = PetMood.Normal;
            else if (hour >= 18 && hour < 22)
                currentMood = intimacy >= 20 ? PetMood.Happy : PetMood.Lonely;
            else
                currentMood = intimacy >= 50 ? PetMood.Normal : PetMood.Lonely;
            if (intimacy >= 100 && rnd.Next(3) == 0)
                currentMood = PetMood.Excited;
        }
        private string GetMoodEmoji() { return moodEmojis[(int)currentMood]; }
        private string GetMoodDescription() { return moodDescriptions[(int)currentMood]; }
        private void MoodTimer_Tick(object sender, EventArgs e) { UpdateMood(); }

        // ==================== 闲聊气泡系统 ====================
        private void ChatTimer_Tick(object sender, EventArgs e)
        {
            if (!this.Visible) return;
            Random rnd = new Random();
            int action = rnd.Next(10);
            if (action < 6)
                ShowChatBubble(chatMessages[rnd.Next(chatMessages.Length)]);
            else if (action < 8)
            {
                string[] moods = { "我现在感觉" + GetMoodDescription() + "~ " + GetMoodEmoji(),
                    GetMoodEmoji() + " " + GetMoodDescription() + "...",
                    "嗯...现在心情是「" + GetMoodDescription() + "」呢~" };
                ShowChatBubble(moods[rnd.Next(moods.Length)]);
            }
            else
            {
                string[] ints = { "现在的亲密度是 " + intimacy + " 哦~",
                    intimacy >= 100 ? "我最喜欢你了！" : "再和我玩玩嘛~",
                    "我们已经是" + GetLevelTitle(int.Parse(GetLevel())) + "关系了呢！",
                    intimacy >= MaxIntimacy ? "亲密度满了！你对我太好了~❤️" : "还要 " + (MaxIntimacy - intimacy) + " 点就满级啦！" };
                ShowChatBubble(ints[rnd.Next(ints.Length)]);
            }
            chatTimer.Interval = rnd.Next(30000, 90000);
        }

        private void ShowChatBubble(string message)
        {
            Form bubble = new Form();
            bubble.FormBorderStyle = FormBorderStyle.None;
            bubble.TopMost = true;
            bubble.ShowInTaskbar = false;
            bubble.StartPosition = FormStartPosition.Manual;
            bubble.BackColor = Color.White;
            bubble.Padding = new Padding(8);

            Panel borderPanel = new Panel();
            borderPanel.BackColor = GetCurrentSkinAccentColor();
            borderPanel.Dock = DockStyle.Fill;
            bubble.Controls.Add(borderPanel);

            Panel innerPanel = new Panel();
            innerPanel.BackColor = Color.White;
            innerPanel.Dock = DockStyle.Fill;
            innerPanel.Padding = new Padding(2);
            borderPanel.Controls.Add(innerPanel);

            Label lbl = new Label();
            lbl.Text = message;
            lbl.Font = new Font("Microsoft YaHei", 9);
            lbl.ForeColor = Color.FromArgb(80, 50, 40);
            lbl.MaximumSize = new Size(180, 0);
            lbl.AutoSize = true;
            lbl.TextAlign = ContentAlignment.MiddleCenter;
            innerPanel.Controls.Add(lbl);

            bubble.Size = new Size(lbl.Width + 24, lbl.Height + 20);

            Point petPos = this.PointToScreen(new Point(0, 0));
            int bubbleX = petPos.X + (this.Width - bubble.Width) / 2;
            int bubbleY = petPos.Y - bubble.Height - 8;
            if (bubbleX < 0) bubbleX = 0;
            if (bubbleX + bubble.Width > Screen.PrimaryScreen.WorkingArea.Width)
                bubbleX = Screen.PrimaryScreen.WorkingArea.Width - bubble.Width;
            if (bubbleY < 0) bubbleY = petPos.Y + this.Height + 4;
            bubble.Location = new Point(bubbleX, bubbleY);

            bubble.Opacity = 0;
            bubble.Show();
            activeBubble = bubble;

            Timer fadeIn = new Timer();
            fadeIn.Interval = 25;
            int step = 0;
            fadeIn.Tick += (s, ev) =>
            {
                step++;
                bubble.Opacity = Math.Min(step * 0.08, 0.95);
                if (step >= 12) { fadeIn.Stop(); }
            };
            fadeIn.Start();

            Timer hold = new Timer();
            hold.Interval = 3500 + message.Length * 50;
            hold.Tick += (s, ev) =>
            {
                hold.Stop();
                Timer fadeOut = new Timer();
                fadeOut.Interval = 25;
                double outStep = 0.95;
                fadeOut.Tick += (s2, ev2) =>
                {
                    outStep -= 0.08;
                    if (outStep <= 0) { fadeOut.Stop(); bubble.Close(); bubble.Dispose(); if (activeBubble == bubble) activeBubble = null; }
                    else bubble.Opacity = outStep;
                };
                fadeOut.Start();
            };
            hold.Start();
        }

        // ==================== 时间问候系统 ====================
        private void GreetingTimer_Tick(object sender, EventArgs e)
        {
            int hour = DateTime.Now.Hour;
            if (hour == lastGreetingHour) return;
            string[] greetings = null;
            bool shouldGreet = false;
            if (hour >= 6 && hour < 9) { greetings = morningGreetings; shouldGreet = true; }
            else if (hour >= 11 && hour < 14) { greetings = noonGreetings; shouldGreet = true; }
            else if (hour >= 17 && hour < 20) { greetings = eveningGreetings; shouldGreet = true; }
            else if (hour >= 22 || hour < 1) { greetings = nightGreetings; shouldGreet = true; }
            if (shouldGreet && !greetedToday)
            {
                lastGreetingHour = hour;
                greetedToday = true;
                Random rnd = new Random();
                ShowChatBubble(greetings[rnd.Next(greetings.Length)]);
                if (hour >= 0 && hour < 1) greetedToday = false;
            }
            else if (hour == 0)
            {
                greetedToday = false;
                lastGreetingHour = -1;
            }
        }

        // ==================== 成就系统 ====================
        public void CheckAndUnlockAchievement(string achievementId)
        {
            if (unlockedAchievements.Contains(achievementId)) return;
            foreach (var ach in allAchievements)
            {
                if (ach.id == achievementId)
                {
                    unlockedAchievements.Add(achievementId);
                    SaveAchievements();
                    ShowAchievementNotification(ach.name, ach.desc, ach.icon);
                    if (achievementForm != null && !achievementForm.IsDisposed)
                        achievementForm.RefreshList();
                    break;
                }
            }
        }

        private void CheckMilestoneAchievements()
        {
            CheckAndUnlockAchievement("first_handshake");
            if (totalHandshakes >= 10) CheckAndUnlockAchievement("handshake10");
            CheckAndUnlockAchievement("first_feed");
            if (totalFeeds >= 10) CheckAndUnlockAchievement("feed10");
            CheckAndUnlockAchievement("first_pat");
            if (totalPats >= 50) CheckAndUnlockAchievement("pat50");
            if (intimacy >= 20) CheckAndUnlockAchievement("intimacy20");
            if (intimacy >= 50) CheckAndUnlockAchievement("intimacy50");
            if (intimacy >= 100) CheckAndUnlockAchievement("intimacy100");
            if (intimacy >= MaxIntimacy) CheckAndUnlockAchievement("intimacy999");
            if (int.Parse(GetLevel()) >= 4) CheckAndUnlockAchievement("level4");
            int hour = DateTime.Now.Hour;
            if (hour >= 23 || hour < 5) CheckAndUnlockAchievement("night_owl");
            if (hour >= 5 && hour < 7) CheckAndUnlockAchievement("early_bird");
        }

        private void ShowAchievementNotification(string name, string desc, string icon)
        {
            Form notify = new Form();
            notify.FormBorderStyle = FormBorderStyle.None;
            notify.TopMost = true;
            notify.ShowInTaskbar = false;
            notify.StartPosition = FormStartPosition.Manual;
            notify.Size = new Size(260, 70);
            notify.BackColor = Color.FromArgb(255, 248, 240);
            notify.Padding = new Padding(2);

            Panel goldBorder = new Panel();
            goldBorder.BackColor = Color.FromArgb(255, 200, 80);
            goldBorder.Dock = DockStyle.Fill;
            notify.Controls.Add(goldBorder);

            Panel inner = new Panel();
            inner.BackColor = Color.FromArgb(255, 248, 240);
            inner.Dock = DockStyle.Fill;
            inner.Padding = new Padding(10);
            goldBorder.Controls.Add(inner);

            Label iconLbl = new Label();
            iconLbl.Text = icon;
            iconLbl.Font = new Font("Segoe UI Emoji", 20);
            iconLbl.Location = new Point(10, 8);
            iconLbl.AutoSize = true;
            inner.Controls.Add(iconLbl);

            Label titleLbl = new Label();
            titleLbl.Text = "🏆 成就解锁！";
            titleLbl.Font = new Font("Microsoft YaHei", 9, FontStyle.Bold);
            titleLbl.ForeColor = Color.FromArgb(200, 150, 50);
            titleLbl.Location = new Point(50, 6);
            titleLbl.Size = new Size(190, 18);
            inner.Controls.Add(titleLbl);

            Label nameLbl = new Label();
            nameLbl.Text = name;
            nameLbl.Font = new Font("Microsoft YaHei", 11, FontStyle.Bold);
            nameLbl.ForeColor = Color.FromArgb(80, 50, 30);
            nameLbl.Location = new Point(50, 26);
            nameLbl.Size = new Size(190, 20);
            inner.Controls.Add(nameLbl);

            Label descLbl = new Label();
            descLbl.Text = desc;
            descLbl.Font = new Font("Microsoft YaHei", 8);
            descLbl.ForeColor = Color.FromArgb(150, 130, 110);
            descLbl.Location = new Point(50, 46);
            descLbl.Size = new Size(190, 16);
            inner.Controls.Add(descLbl);

            int screenW = Screen.PrimaryScreen.WorkingArea.Width;
            notify.Location = new Point(screenW - notify.Width - 15, 15);
            notify.Opacity = 0;
            notify.Show();

            Timer fadeIn = new Timer();
            fadeIn.Interval = 20;
            int fStep = 0;
            fadeIn.Tick += (s, ev) =>
            {
                fStep++;
                notify.Opacity = Math.Min(fStep * 0.05, 1.0);
                if (fStep >= 20) fadeIn.Stop();
            };
            fadeIn.Start();

            Timer hold = new Timer();
            hold.Interval = 4000;
            hold.Tick += (s, ev) =>
            {
                hold.Stop();
                Timer fadeOut = new Timer();
                fadeOut.Interval = 20;
                int oStep = 20;
                fadeOut.Tick += (s2, ev2) =>
                {
                    oStep--;
                    notify.Opacity = oStep * 0.05;
                    if (oStep <= 0) { fadeOut.Stop(); notify.Close(); notify.Dispose(); }
                };
                fadeOut.Start();
            };
            hold.Start();
        }

        // ==================== 皮肤系统 ====================
        private void SelectSkin(int index)
        {
            if (index < 0 || index >= skinNames.Length) return;
            currentSkinIndex = index;
            UpdateSkinMenuCheck();
            ShowFloatingText(skinNames[currentSkinIndex], GetCurrentSkinAccentColor());
            ApplyCurrentSkin();
            if (detailsForm != null && !detailsForm.IsDisposed) detailsForm.RefreshUI();
        }
        private void UpdateSkinMenuCheck()
        {
            for (int i = 0; i < petMenu.Items.Count; i++)
            {
                ToolStripMenuItem si = petMenu.Items[i] as ToolStripMenuItem;
                if (si != null && si.Text == "切换皮肤")
                {
                    for (int j = 0; j < si.DropDownItems.Count; j++)
                        ((ToolStripMenuItem)si.DropDownItems[j]).Checked = (j == currentSkinIndex);
                    break;
                }
            }
        }
        private void CycleSkin(object sender, EventArgs e)
        {
            currentSkinIndex = (currentSkinIndex + 1) % skinNames.Length;
            UpdateSkinMenuCheck();
            ShowFloatingText(skinNames[currentSkinIndex], GetCurrentSkinAccentColor());
            ApplyCurrentSkin();
            if (detailsForm != null && !detailsForm.IsDisposed) detailsForm.RefreshUI();
        }
        public Color GetCurrentSkinPrimaryColor() { return skinColors[currentSkinIndex][0]; }
        public Color GetCurrentSkinSecondaryColor() { return skinColors[currentSkinIndex][1]; }
        public Color GetCurrentSkinBgColor() { return skinColors[currentSkinIndex][2]; }
        private Color GetCurrentSkinAccentColor() { return skinColors[currentSkinIndex][0]; }
        private void ApplyCurrentSkin() { /* 可扩展 */ }

        // ==================== 昼夜模式 ====================
        private void UpdateDayNightMode()
        {
            int hour = DateTime.Now.Hour;
            bool shouldBeNight = (hour >= 19 || hour < 6);
            if (shouldBeNight != isNightMode)
            {
                isNightMode = shouldBeNight;
                this.Opacity = isNightMode ? 0.85 : 1.0;
            }
        }

        // ==================== 面板打开方法 ====================
        public void OpenStats(object sender, EventArgs e)
        {
            if (statsForm == null || statsForm.IsDisposed) statsForm = new StatsForm(this);
            else statsForm.RefreshData();
            statsForm.Show(); statsForm.BringToFront();
        }
        public void OpenAchievements(object sender, EventArgs e)
        {
            if (achievementForm == null || achievementForm.IsDisposed) achievementForm = new AchievementForm(this);
            else achievementForm.RefreshList();
            achievementForm.Show(); achievementForm.BringToFront();
        }
        public void OpenQuests(object sender, EventArgs e)
        {
            CheckDailyReset();
            if (questForm == null || questForm.IsDisposed) questForm = new QuestForm(this);
            else questForm.RefreshUI();
            questForm.Show(); questForm.BringToFront();
        }

        private ShopForm shopForm;
        private GameForm gameForm;
        public void OpenShop()
        {
            if (shopForm == null || shopForm.IsDisposed) shopForm = new ShopForm(this);
            else shopForm.RefreshUI();
            shopForm.Show(); shopForm.BringToFront();
        }

        public void OpenGameSelect()
        {
            if (gameForm == null || gameForm.IsDisposed) gameForm = new GameForm(this);
            gameForm.Show(); gameForm.BringToFront();
        }

        // 推箱子游戏进度保存
        private string sokobanSavePath = Path.Combine(Application.StartupPath, "pet_sokoban.txt");
        public int SokobanUnlockedLevel = 1;
        public bool[] SokobanRewarded = new bool[11]; // index 1-10
        public void LoadSokobanProgress()
        {
            try
            {
                if (File.Exists(sokobanSavePath))
                {
                    string[] lines = File.ReadAllLines(sokobanSavePath);
                    if (lines.Length >= 2)
                    {
                        Int32.TryParse(lines[0], out SokobanUnlockedLevel);
                        if (SokobanUnlockedLevel < 1) SokobanUnlockedLevel = 1;
                        if (SokobanUnlockedLevel > 10) SokobanUnlockedLevel = 10;
                        string[] rewards = lines[1].Split(',');
                        for (int i = 1; i <= 10 && i < rewards.Length; i++)
                        {
                            bool val; Boolean.TryParse(rewards[i], out val);
                            SokobanRewarded[i] = val;
                        }
                    }
                }
            }
            catch { }
        }
        public void SaveSokobanProgress()
        {
            try
            {
                string rewards = string.Join(",", new string[] { "", SokobanRewarded[1].ToString(), SokobanRewarded[2].ToString(), SokobanRewarded[3].ToString(), SokobanRewarded[4].ToString(), SokobanRewarded[5].ToString(), SokobanRewarded[6].ToString(), SokobanRewarded[7].ToString(), SokobanRewarded[8].ToString(), SokobanRewarded[9].ToString(), SokobanRewarded[10].ToString() });
                File.WriteAllText(sokobanSavePath, SokobanUnlockedLevel + "\n" + rewards);
            }
            catch { }
        }

        // ==================== 原有功能（保留+增强）====================
        public void UpdatePetToolTip()
        {
            string level = GetLevel();
            petToolTip.SetToolTip(this, petName + " | Lv." + level + " " + GetMoodEmoji() + " 亲密度: " + intimacy);
        }
        public int Intimacy { get { return intimacy; } set { intimacy = value; } }
        public string PetName { get { return petName; } set { petName = value; } }
        public string GetLevel()
        {
            if (intimacy >= 100) return "4";
            if (intimacy >= 50) return "3";
            if (intimacy >= 20) return "2";
            return "1";
        }
        public int GetLevelExp()
        {
            if (intimacy >= 100) return (intimacy - 100);
            if (intimacy >= 50) return (intimacy - 50);
            if (intimacy >= 20) return (intimacy - 20);
            return intimacy;
        }
        public int GetLevelMax()
        {
            if (intimacy >= 100) return MaxIntimacy - 100;
            if (intimacy >= 50) return 50;
            if (intimacy >= 20) return 30;
            return 20;
        }
        public bool CanHandshake() { return intimacy < MaxIntimacy && (DateTime.Now - lastHandshake).TotalHours >= HandCdHours; }
        public string GetHandCdRemaining()
        {
            if (intimacy >= MaxIntimacy) return "亲密度已满";
            double remain = HandCdHours - (DateTime.Now - lastHandshake).TotalHours;
            if (remain <= 0) return "就绪";
            int min = (int)(remain * 60);
            return min < 60 ? min + "分钟" : (min / 60) + "小时" + (min % 60) + "分钟";
        }
        public void DoHandshake()
        {
            if (intimacy >= MaxIntimacy) return;
            intimacy += 5;
            if (intimacy > MaxIntimacy) intimacy = MaxIntimacy;
            lastHandshake = DateTime.Now;
            totalHandshakes++; RecordInteraction();
            SaveCd(); SaveStats(); UpdatePetToolTip();
            CheckDailyReset(); UpdateQuestProgress("daily_handshake", 1); UpdateQuestProgress("daily_interact", 1); UpdateMilestoneQuests();
            Random rnd = new Random();
            petToolTip.SetToolTip(this, handReactions[rnd.Next(handReactions.Length)]);
            reactionTimer.Start(); SaveIntimacy(); AnimateShake();
            ShowFloatingText("+5", Color.FromArgb(255, 130, 80));
            CheckMilestoneAchievements();
            if (detailsForm != null && !detailsForm.IsDisposed) detailsForm.RefreshUI();
            RefreshHoverMenu();
        }
        public bool CanFeed() { return intimacy < MaxIntimacy && (DateTime.Now - lastFeed).TotalHours >= FeedCdHours; }
        public DateTime GetLastFeedTime() { return lastFeed; }
        public string GetFeedCdRemaining()
        {
            if (intimacy >= MaxIntimacy) return "亲密度已满";
            double remain = FeedCdHours - (DateTime.Now - lastFeed).TotalHours;
            if (remain <= 0) return "就绪";
            int min = (int)(remain * 60);
            return min < 60 ? min + "分钟" : (min / 60) + "小时" + (min % 60) + "分钟";
        }
        public void DoFeed()
        {
            if (intimacy >= MaxIntimacy) return;
            intimacy += 10;
            if (intimacy > MaxIntimacy) intimacy = MaxIntimacy;
            lastFeed = DateTime.Now;
            totalFeeds++; RecordInteraction();
            SaveCd(); SaveStats(); UpdatePetToolTip();
            CheckDailyReset(); UpdateQuestProgress("daily_feed", 1); UpdateQuestProgress("daily_interact", 1); UpdateMilestoneQuests();
            Random rnd = new Random();
            petToolTip.SetToolTip(this, feedReactions[rnd.Next(feedReactions.Length)]);
            reactionTimer.Start(); SaveIntimacy(); AnimateBounce();
            ShowFloatingText("+10", Color.FromArgb(100, 180, 120));
            CheckMilestoneAchievements();
            if (detailsForm != null && !detailsForm.IsDisposed) detailsForm.RefreshUI();
            RefreshHoverMenu();
        }
        private bool CanPat() { return intimacy < MaxIntimacy && (DateTime.Now - patLastTime).TotalSeconds >= PatCdSeconds; }
        private void DoPat()
        {
            if (!CanPat()) return;
            intimacy += 3;
            if (intimacy > MaxIntimacy) intimacy = MaxIntimacy;
            patLastTime = DateTime.Now;
            totalPats++; RecordInteraction();
            SaveStats(); UpdatePetToolTip();
            CheckDailyReset(); UpdateQuestProgress("daily_pat", 1); UpdateQuestProgress("daily_interact", 1); UpdateMilestoneQuests();
            Random rnd = new Random();
            petToolTip.SetToolTip(this, patReactions[rnd.Next(patReactions.Length)]);
            reactionTimer.Start(); SaveIntimacy(); AnimateBounce();
            ShowFloatingText("+3", Color.FromArgb(255, 100, 160));
            CheckMilestoneAchievements();
            if (detailsForm != null && !detailsForm.IsDisposed) detailsForm.RefreshUI();
            RefreshHoverMenu();
        }
        private void OnDoubleClickPet(object sender, EventArgs e) { DoPat(); }

        private void ShowFloatingText(string text, Color color)
        {
            Label floatLabel = new Label();
            floatLabel.Text = text;
            floatLabel.Font = new Font("Microsoft YaHei", 14, FontStyle.Bold);
            floatLabel.ForeColor = color;
            floatLabel.BackColor = Color.Transparent;
            floatLabel.AutoSize = true;
            floatLabel.Location = new Point(40, 50);
            this.Controls.Add(floatLabel); floatLabel.BringToFront();
            Timer fadeTimer = new Timer();
            fadeTimer.Interval = 35;
            int step = 0;
            fadeTimer.Tick += (s, ev) =>
            {
                step++;
                floatLabel.Top -= 2;
                if (step >= 18) { fadeTimer.Stop(); this.Controls.Remove(floatLabel); floatLabel.Dispose(); }
            };
            fadeTimer.Start();
        }

        private void IdleTimer_Tick(object sender, EventArgs e)
        {
            idleTimer.Stop();
            UpdateDayNightMode();
            Random rnd = new Random();
            int action = rnd.Next(5);
            if (action == 0) AnimateShake();
            else if (action == 1) AnimateBounce();
            else if (action == 2) AnimateSpin();
            else if (action == 3) AnimateWave();
            idleTimer.Interval = rnd.Next(15000, 35000);
            idleTimer.Start();
        }

        private void AnimateSpin()
        {
            for (int i = 0; i < 4; i++)
            {
                float scale = (i % 2 == 0) ? 0.9f : 1.0f;
                int w = (int)(128 * scale), h = (int)(128 * scale);
                this.ClientSize = new Size(w, h);
                Application.DoEvents(); System.Threading.Thread.Sleep(50);
            }
            this.ClientSize = new Size(128, 128);
        }
        private void AnimateWave()
        {
            Point original = this.Location;
            for (int i = 0; i < 6; i++)
            {
                int offset = (i % 2 == 0) ? -8 : 0;
                this.Location = new Point(original.X + offset, original.Y);
                Application.DoEvents(); System.Threading.Thread.Sleep(35);
            }
            this.Location = original;
        }

        public void OpenDetails(object sender, EventArgs e)
        {
            if (detailsForm == null || detailsForm.IsDisposed) detailsForm = new PetDetailsForm(this);
            detailsForm.Show(); detailsForm.BringToFront();
        }

        private Bitmap RemoveBlackBackground(string path)
        {
            Bitmap src = new Bitmap(path);
            int w = src.Width, h = src.Height;
            Bitmap dst = new Bitmap(w, h, PixelFormat.Format24bppRgb);
            BitmapData srcData = src.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            BitmapData dstData = dst.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            int stride = srcData.Stride;
            byte[] srcBytes = new byte[stride * h], dstBytes = new byte[stride * h];
            System.Runtime.InteropServices.Marshal.Copy(srcData.Scan0, srcBytes, 0, srcBytes.Length);
            byte fR = TranspColor.R, fG = TranspColor.G, fB = TranspColor.B;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int idx = y * stride + x * 3;
                    byte b = srcBytes[idx], g = srcBytes[idx + 1], r = srcBytes[idx + 2];
                    bool isEdge = (x < 3 || x >= w - 3 || y < 3 || y >= h - 3);
                    int threshold = isEdge ? 80 : BlackThreshold;
                    if (r < threshold && g < threshold && b < threshold)
                    { dstBytes[idx] = fB; dstBytes[idx + 1] = fG; dstBytes[idx + 2] = fR; }
                    else { dstBytes[idx] = b; dstBytes[idx + 1] = g; dstBytes[idx + 2] = r; }
                }
            System.Runtime.InteropServices.Marshal.Copy(dstBytes, 0, dstData.Scan0, dstBytes.Length);
            dst.UnlockBits(dstData); src.UnlockBits(srcData); src.Dispose();
            return dst;
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; this.Hide(); }
            else { SaveAll(); trayIcon.Visible = false; trayIcon.Dispose(); }
        }
        private void ShowPet(object sender, EventArgs e) { this.Show(); this.WindowState = FormWindowState.Normal; }
        private void ResetPosition(object sender, EventArgs e)
        {
            int sW = Screen.PrimaryScreen.WorkingArea.Width, sH = Screen.PrimaryScreen.WorkingArea.Height;
            this.Location = new Point(sW - 148, sH - 170); this.Show(); this.WindowState = FormWindowState.Normal;
        }
        private void ShowStartupBubble()
        {
            Form bubble = new Form();
            bubble.FormBorderStyle = FormBorderStyle.None; bubble.TopMost = true; bubble.ShowInTaskbar = false;
            bubble.StartPosition = FormStartPosition.Manual; bubble.BackColor = GetCurrentSkinPrimaryColor();
            bubble.Size = new Size(180, 44);
            int sW = Screen.PrimaryScreen.WorkingArea.Width;
            bubble.Location = new Point((sW - 180) / 2, 40);
            Label lbl = new Label(); lbl.Text = "🐾 " + petName + " 来啦！";
            lbl.Font = new Font("Microsoft YaHei", 11, FontStyle.Bold); lbl.ForeColor = Color.White;
            lbl.TextAlign = ContentAlignment.MiddleCenter; lbl.Dock = DockStyle.Fill; bubble.Controls.Add(lbl);
            bubble.Opacity = 0; bubble.Show();
            Timer fi = new Timer(); fi.Interval = 30; int stp = 0;
            fi.Tick += (s, ev) => { stp++; bubble.Opacity = stp * 0.05; if (stp >= 20) fi.Stop(); }; fi.Start();
            Timer hd = new Timer(); hd.Interval = 2000;
            hd.Tick += (s, ev) => { hd.Stop(); Timer fo = new Timer(); fo.Interval = 30; int os = 20;
                fo.Tick += (s2, ev2) => { os--; bubble.Opacity = os * 0.05; if (os <= 0) { fo.Stop(); bubble.Close(); bubble.Dispose(); } }; fo.Start(); }; hd.Start();
        }
        private void ExitPet(object sender, EventArgs e)
        {
            SaveAll();
            if (detailsForm != null && !detailsForm.IsDisposed) detailsForm.Close();
            if (statsForm != null && !statsForm.IsDisposed) statsForm.Close();
            if (achievementForm != null && !achievementForm.IsDisposed) achievementForm.Close();
            trayIcon.Visible = false; trayIcon.Dispose(); Environment.Exit(0);
        }
        private void AnimateShake()
        {
            Point original = this.Location;
            for (int i = 0; i < 3; i++) { this.Location = new Point(original.X + (i % 2 == 0 ? 3 : -3), original.Y); Application.DoEvents(); System.Threading.Thread.Sleep(30); }
            this.Location = original;
        }
        private void AnimateBounce()
        {
            Point original = this.Location;
            for (int i = 0; i < 4; i++) { int off = (i % 2 == 0) ? -5 : 5; this.Location = new Point(original.X, original.Y + off); Application.DoEvents(); System.Threading.Thread.Sleep(40); }
            this.Location = original;
        }
        private string GetIdleMessage()
        {
            if (intimacy >= 100) return "最喜欢你啦~ " + GetMoodEmoji();
            if (intimacy >= 50) return "好开心~ " + GetMoodEmoji();
            if (intimacy >= 20) return "好朋友~ " + GetMoodEmoji();
            if (intimacy >= 10) return "感到温暖了~ " + GetMoodEmoji();
            return "亲密度: " + intimacy + " " + GetMoodEmoji();
        }

        // 公开属性供子窗体调用
        public string GetCurrentMoodDescription() { return GetMoodDescription(); }
        public string GetCurrentMoodEmoji() { return GetMoodEmoji(); }
        public string GetCurrentSkinName() { return skinNames[currentSkinIndex]; }
        public int TotalHandshakes { get { return totalHandshakes; } }
        public int TotalFeeds { get { return totalFeeds; } }
        public int TotalPats { get { return totalPats; } }
        public int TotalDaysActive { get { if (firstInteractionDate == DateTime.MinValue) return 1; return Math.Max(1, (DateTime.Now.Date - firstInteractionDate.Date).Days + 1); } }
        public HashSet<string> UnlockedAchievements { get { return unlockedAchievements; } }
        public AchievementDef[] AllAchievements { get { return allAchievements; } }
        public bool IsNightMode { get { return isNightMode; } }
        public static string GetLevelTitle(int level)
        {
            switch (level) { case 4: return "灵魂伴侣"; case 3: return "好朋友"; case 2: return "熟悉中"; default: return "初识"; }
        }

        private void RecordInteraction() { if (firstInteractionDate == DateTime.MinValue) firstInteractionDate = DateTime.Now; lastInteractionDate = DateTime.Now; }

        // 数据持久化
        private void SaveIntimacy() { try { File.WriteAllText(savePath, intimacy.ToString()); } catch { } }
        private void SaveName() { try { File.WriteAllText(nameSavePath, petName); } catch { } }
        private void SaveAll() { SaveIntimacy(); SaveName(); SaveCd(); SaveStats(); SaveAchievements(); }
        private void SaveCd() { try { File.WriteAllText(handCdPath, lastHandshake.Ticks.ToString()); File.WriteAllText(feedCdPath, lastFeed.Ticks.ToString()); } catch { } }
        private void SaveStats()
        {
            try { File.WriteAllText(statsPath, string.Format("{0}|{1}|{2}|0|{3}|{4}", totalHandshakes, totalFeeds, totalPats, firstInteractionDate.ToString("o"), lastInteractionDate.ToString("o"))); } catch { }
        }
        private void LoadStats()
        {
            try { if (File.Exists(statsPath)) { string[] p = File.ReadAllText(statsPath).Split('|'); if (p.Length >= 4) { int.TryParse(p[0], out totalHandshakes); int.TryParse(p[1], out totalFeeds); int.TryParse(p[2], out totalPats); if (p.Length >= 6) { DateTime.TryParse(p[4], out firstInteractionDate); DateTime.TryParse(p[5], out lastInteractionDate); } } } } catch { }
        }
        public void ResetAllData()
        {
            try { File.Delete(Path.Combine(Application.StartupPath, "pet_intimacy.txt")); } catch { }
            try { File.Delete(Path.Combine(Application.StartupPath, "pet_name.txt")); } catch { }
            try { File.Delete(Path.Combine(Application.StartupPath, "pet_handshake_cd.txt")); } catch { }
            try { File.Delete(Path.Combine(Application.StartupPath, "pet_feed_cd.txt")); } catch { }
            try { File.Delete(statsPath); } catch { }
            try { File.Delete(Path.Combine(Application.StartupPath, "pet_achievements.txt")); } catch { }
            try { File.Delete(questsPath); } catch { }
            try { File.Delete(crystalsPath); } catch { }
            try { File.Delete(milestoneQuestsPath); } catch { }
            try { File.Delete(Path.Combine(Application.StartupPath, "pet_foodstock.txt")); } catch { }
            try { File.Delete(Path.Combine(Application.StartupPath, "pet_shoponly.txt")); } catch { }
            try { File.Delete(sokobanSavePath); } catch { }
            intimacy = 0; petName = "小宠物"; currentSkinIndex = 0;
            lastHandshake = DateTime.MinValue; lastFeed = DateTime.MinValue;
            unlockedAchievements.Clear(); totalHandshakes = 0; totalFeeds = 0; totalPats = 0;
            crystals = 0; questProgress.Clear(); questClaimed.Clear(); milestoneProgress.Clear(); milestoneClaimed.Clear();
            for (int i = 0; i < foodStock.Length; i++) foodStock[i] = 0;
            for (int i = 0; i < shopOnlyPurchased.Length; i++) shopOnlyPurchased[i] = false;
            SokobanUnlockedLevel = 1;
            for (int i = 0; i < SokobanRewarded.Length; i++) SokobanRewarded[i] = false;
            firstInteractionDate = DateTime.MinValue; lastInteractionDate = DateTime.Now;
            SaveIntimacy(); UpdatePetToolTip();
        }
        private void SaveAchievements() { try { File.WriteAllText(achievementsPath, string.Join(",", unlockedAchievements)); } catch { } }
        private void LoadAchievements()
        {
            try { if (File.Exists(achievementsPath)) { string d = File.ReadAllText(achievementsPath).Trim(); if (!string.IsNullOrEmpty(d)) foreach (string id in d.Split(',')) if (!string.IsNullOrEmpty(id.Trim())) unlockedAchievements.Add(id.Trim()); } } catch { }
        }
        private void LoadCd()
        {
            try { if (File.Exists(handCdPath)) { long t; if (long.TryParse(File.ReadAllText(handCdPath), out t)) lastHandshake = new DateTime(t); } if (File.Exists(feedCdPath)) { long t; if (long.TryParse(File.ReadAllText(feedCdPath), out t)) lastFeed = new DateTime(t); } } catch { }
        }
        private void LoadData()
        {
            try { if (File.Exists(savePath)) { int s; if (int.TryParse(File.ReadAllText(savePath), out s)) intimacy = s; } if (File.Exists(nameSavePath)) { string n = File.ReadAllText(nameSavePath).Trim(); if (!string.IsNullOrEmpty(n)) petName = n; } } catch { }
            UpdatePetToolTip(); LoadCd(); LoadStats(); LoadAchievements(); LoadQuests(); LoadCrystals(); LoadMilestoneQuests(); LoadFoodStock(); LoadShopOnly(); LoadSokobanProgress(); UpdateMood();
        }

        // ==================== 任务系统核心逻辑 ====================
        private void CheckDailyReset()
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            if (questCompletedDate != today)
            {
                questCompletedDate = today;
                questProgress.Clear();
                questClaimed.Clear();
                foreach (var q in questDefs)
                {
                    questProgress[q.id] = 0;
                    questClaimed[q.id] = false;
                }
                questProgress["daily_login"] = 1;
                SaveQuests();
                SaveCrystals();
            }
            // 确保所有条目都存在（防止文件不完整导致领取失败）
            foreach (var q in questDefs)
            {
                if (!questProgress.ContainsKey(q.id)) questProgress[q.id] = 0;
                if (!questClaimed.ContainsKey(q.id)) questClaimed[q.id] = false;
            }
        }
        public void UpdateQuestProgress(string questId, int amount)
        {
            if (!questProgress.ContainsKey(questId)) questProgress[questId] = 0;
            if (!questClaimed.ContainsKey(questId)) questClaimed[questId] = false;
            QuestDef def = GetQuestDef(questId);
            if (def.id != null && questProgress[questId] >= def.target) return;
            questProgress[questId] = questProgress[questId] + amount;
            if (def.id != null && questProgress[questId] > def.target) questProgress[questId] = def.target;
            SaveQuests();
            if (questForm != null && !questForm.IsDisposed) questForm.RefreshUI();
        }
        public bool CanClaimQuest(string questId)
        {
            if (!questProgress.ContainsKey(questId) || !questClaimed.ContainsKey(questId)) return false;
            QuestDef def = GetQuestDef(questId);
            if (def.id == null) return false;
            return questProgress[questId] >= def.target && !questClaimed[questId];
        }
        public void ClaimQuest(string questId)
        {
            if (!CanClaimQuest(questId)) return;
            QuestDef def = GetQuestDef(questId);
            questClaimed[questId] = true;
            crystals = crystals + def.reward;
            SaveQuests();
            SaveCrystals();
            if (questForm != null && !questForm.IsDisposed) questForm.RefreshUI();
        }
        public void DoDailyCheckin()
        {
            if (questClaimed.ContainsKey("daily_login") && questClaimed["daily_login"]) return;
            QuestDef def = GetQuestDef("daily_login");
            questProgress["daily_login"] = def.target;
            questClaimed["daily_login"] = true;
            crystals = crystals + def.reward;
            SaveQuests();
            SaveCrystals();
            if (questForm != null && !questForm.IsDisposed) questForm.RefreshUI();
        }
        public int GetQuestProgress(string questId)
        {
            if (questProgress.ContainsKey(questId)) return questProgress[questId];
            return 0;
        }
        public bool IsQuestClaimed(string questId)
        {
            if (questClaimed.ContainsKey(questId)) return questClaimed[questId];
            return false;
        }
        public QuestDef GetQuestDef(string questId)
        {
            foreach (var q in questDefs) { if (q.id == questId) return q; }
            return new QuestDef();
        }
        public QuestDef[] GetAllQuestDefs() { return questDefs; }
        public int GetCrystals() { return crystals; }

        // ==================== 食物系统 ====================
        public struct FoodDef { public string name; public string icon; public int price; public int intimacy; }
        public FoodDef[] foodDefs = new FoodDef[]
        {
            new FoodDef{ name="面包", icon="🍞", price=0, intimacy=5 },
            new FoodDef{ name="苹果", icon="🍎", price=5, intimacy=8 },
            new FoodDef{ name="肉骨头", icon="🍖", price=10, intimacy=12 },
            new FoodDef{ name="蛋糕", icon="🎂", price=20, intimacy=18 },
            new FoodDef{ name="寿司", icon="🍣", price=30, intimacy=25 },
        };
        public int[] foodStock = new int[5];
        public int[] foodMaxStock = new int[] { 99, 99, 99, 99, 99 };

        public void LoadFoodStock()
        {
            for (int i = 0; i < foodStock.Length; i++) foodStock[i] = 0;
            string path = Path.Combine(Application.StartupPath, "pet_foodstock.txt");
            if (File.Exists(path))
            {
                try
                {
                    string[] lines = File.ReadAllText(path).Split(',');
                    for (int i = 0; i < lines.Length && i < foodStock.Length; i++)
                    {
                        int s; if (int.TryParse(lines[i], out s)) foodStock[i] = s;
                    }
                }
                catch { }
            }
        }
        public void SaveFoodStock()
        {
            string path = Path.Combine(Application.StartupPath, "pet_foodstock.txt");
            try { File.WriteAllText(path, string.Join(",", foodStock)); } catch { }
        }
        public int GetFoodStock(int index) { if (index >= 0 && index < foodStock.Length) return foodStock[index]; return 0; }

        // ==================== 商城专属物品 ====================
        public struct ShopOnlyDef { public string name; public string icon; public int price; }
        public ShopOnlyDef[] shopOnlyDefs = new ShopOnlyDef[]
        {
            new ShopOnlyDef{ name="五元红包", icon="🧧", price=250 },
        };
        public bool[] shopOnlyPurchased = new bool[1];

        public void LoadShopOnly()
        {
            for (int i = 0; i < shopOnlyPurchased.Length; i++) shopOnlyPurchased[i] = false;
            string path = Path.Combine(Application.StartupPath, "pet_shoponly.txt");
            if (File.Exists(path))
            {
                try
                {
                    string[] lines = File.ReadAllText(path).Split(',');
                    for (int i = 0; i < lines.Length && i < shopOnlyPurchased.Length; i++)
                    {
                        int v; if (int.TryParse(lines[i], out v)) shopOnlyPurchased[i] = (v == 1);
                    }
                }
                catch { }
            }
        }
        public void SaveShopOnly()
        {
            string[] vals = new string[shopOnlyPurchased.Length];
            for (int i = 0; i < vals.Length; i++) vals[i] = shopOnlyPurchased[i] ? "1" : "0";
            string path = Path.Combine(Application.StartupPath, "pet_shoponly.txt");
            try { File.WriteAllText(path, string.Join(",", vals)); } catch { }
        }
        public bool BuyShopOnly(int index)
        {
            if (index < 0 || index >= shopOnlyDefs.Length) return false;
            if (shopOnlyPurchased[index]) return false;
            if (crystals < shopOnlyDefs[index].price) return false;
            crystals = crystals - shopOnlyDefs[index].price;
            SaveCrystals();
            shopOnlyPurchased[index] = true;
            SaveShopOnly();
            MessageBox.Show("恭喜！请找开发者领取五元红包！🧧", "兑换成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return true;
        }

        public bool BuyFromShop(int foodIndex)
        {
            if (foodIndex < 0 || foodIndex >= foodDefs.Length) return false;
            if (foodStock[foodIndex] >= foodMaxStock[foodIndex]) return false;
            if (crystals < foodDefs[foodIndex].price) return false;
            crystals = crystals - foodDefs[foodIndex].price;
            SaveCrystals();
            foodStock[foodIndex]++;
            SaveFoodStock();
            if (questForm != null && !questForm.IsDisposed) questForm.RefreshUI();
            return true;
        }

        public bool BuyFood(FoodDef food)
        {
            if (intimacy >= MaxIntimacy) return false;
            if ((DateTime.Now - lastFeed).TotalHours < FeedCdHours) return false;
            int foodIdx = -1;
            for (int i = 0; i < foodDefs.Length; i++) { if (foodDefs[i].name == food.name) { foodIdx = i; break; } }
            if (food.price > 0)
            {
                if (foodIdx >= 0 && foodStock[foodIdx] <= 0) return false;
                if (foodIdx >= 0) { foodStock[foodIdx]--; SaveFoodStock(); }
            }

            intimacy += food.intimacy;
            if (intimacy > MaxIntimacy) intimacy = MaxIntimacy;
            lastFeed = DateTime.Now;
            totalFeeds++; RecordInteraction();
            SaveCd(); SaveStats(); UpdatePetToolTip();
            CheckDailyReset(); UpdateQuestProgress("daily_feed", 1); UpdateQuestProgress("daily_interact", 1); UpdateMilestoneQuests();
            Random rnd = new Random();
            petToolTip.SetToolTip(this, feedReactions[rnd.Next(feedReactions.Length)]);
            reactionTimer.Start(); SaveIntimacy(); AnimateBounce();
            ShowFloatingText("+" + food.intimacy, Color.FromArgb(100, 180, 120));
            CheckMilestoneAchievements();
            if (detailsForm != null && !detailsForm.IsDisposed) detailsForm.RefreshUI();
            if (questForm != null && !questForm.IsDisposed) questForm.RefreshUI();
            RefreshHoverMenu();
            return true;
        }
        private void SaveQuests()
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(questsPath))
                {
                    sw.WriteLine(questCompletedDate);
                    foreach (var q in questDefs)
                    {
                        string pid = q.id;
                        int prog = questProgress.ContainsKey(pid) ? questProgress[pid] : 0;
                        bool claimed = questClaimed.ContainsKey(pid) ? questClaimed[pid] : false;
                        sw.WriteLine(pid + "|" + prog + "|" + (claimed ? "1" : "0"));
                    }
                }
            }
            catch { }
        }
        private void LoadQuests()
        {
            try
            {
                if (File.Exists(questsPath))
                {
                    string[] lines = File.ReadAllLines(questsPath);
                    if (lines.Length > 0) questCompletedDate = lines[0];
                    for (int i = 1; i < lines.Length; i++)
                    {
                        string[] parts = lines[i].Split('|');
                        if (parts.Length >= 3)
                        {
                            questProgress[parts[0]] = int.Parse(parts[1]);
                            questClaimed[parts[0]] = parts[2] == "1";
                        }
                    }
                }
            }
            catch { }
            CheckDailyReset();
        }
        private void SaveCrystals()
        {
            try { File.WriteAllText(crystalsPath, crystals.ToString()); } catch { }
        }
        public void AddCrystals(int amount)
        {
            crystals += amount;
            SaveCrystals();
        }
        private void LoadCrystals()
        {
            try { if (File.Exists(crystalsPath)) { int c; if (int.TryParse(File.ReadAllText(crystalsPath), out c)) crystals = c; } } catch { }
        }

        // ==================== 里程碑任务逻辑 ====================
        public void UpdateMilestoneQuests()
        {
            int days = TotalDaysActive;
            int hs = totalHandshakes;
            int fs = totalFeeds;
            int ps = totalPats;
            foreach (var q in milestoneQuestDefs)
            {
                int val = 0;
                if (q.id.StartsWith("ms_pat_")) val = ps;
                else if (q.id.StartsWith("ms_hand_")) val = hs;
                else if (q.id.StartsWith("ms_feed_")) val = fs;
                else if (q.id.StartsWith("ms_days_")) val = days;
                if (!milestoneProgress.ContainsKey(q.id)) milestoneProgress[q.id] = 0;
                if (val > milestoneProgress[q.id]) milestoneProgress[q.id] = val;
                if (milestoneProgress[q.id] > q.target) milestoneProgress[q.id] = q.target;
            }
            SaveMilestoneQuests();
            if (questForm != null && !questForm.IsDisposed) questForm.RefreshUI();
        }
        public int GetMilestoneQuestProgress(string questId)
        {
            if (milestoneProgress.ContainsKey(questId)) return milestoneProgress[questId];
            return 0;
        }
        public bool IsMilestoneQuestClaimed(string questId)
        {
            if (milestoneClaimed.ContainsKey(questId)) return milestoneClaimed[questId];
            return false;
        }
        public bool CanClaimMilestoneQuest(string questId)
        {
            if (!milestoneProgress.ContainsKey(questId) || !milestoneClaimed.ContainsKey(questId)) return false;
            QuestDef def = GetMilestoneQuestDef(questId);
            if (def.id == null) return false;
            return milestoneProgress[questId] >= def.target && !milestoneClaimed[questId];
        }
        public void ClaimMilestoneQuest(string questId)
        {
            if (!CanClaimMilestoneQuest(questId)) return;
            QuestDef def = GetMilestoneQuestDef(questId);
            milestoneClaimed[questId] = true;
            crystals = crystals + def.reward;
            SaveMilestoneQuests();
            SaveCrystals();
            if (questForm != null && !questForm.IsDisposed) questForm.RefreshUI();
        }
        public QuestDef GetMilestoneQuestDef(string questId)
        {
            foreach (var q in milestoneQuestDefs) { if (q.id == questId) return q; }
            return new QuestDef();
        }
        public QuestDef[] GetAllMilestoneQuestDefs() { return milestoneQuestDefs; }
        private void SaveMilestoneQuests()
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(milestoneQuestsPath))
                {
                    foreach (var q in milestoneQuestDefs)
                    {
                        string pid = q.id;
                        int prog = milestoneProgress.ContainsKey(pid) ? milestoneProgress[pid] : 0;
                        bool claimed = milestoneClaimed.ContainsKey(pid) ? milestoneClaimed[pid] : false;
                        sw.WriteLine(pid + "|" + prog + "|" + (claimed ? "1" : "0"));
                    }
                }
            }
            catch { }
        }
        public void LoadMilestoneQuests()
        {
            try
            {
                if (File.Exists(milestoneQuestsPath))
                {
                    string[] lines = File.ReadAllLines(milestoneQuestsPath);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        string[] parts = lines[i].Split('|');
                        if (parts.Length >= 3)
                        {
                            milestoneProgress[parts[0]] = int.Parse(parts[1]);
                            milestoneClaimed[parts[0]] = parts[2] == "1";
                        }
                    }
                }
            }
            catch { }
            UpdateMilestoneQuests();
        }

        private void HoverTimer_Tick(object sender, EventArgs e)
        {
            if (isDragging) return;

            Point mp = Cursor.Position;
            bool menuOn = (hoverMenu != null && !hoverMenu.IsDisposed && hoverMenu.Visible);
            Rectangle petRect = this.RectangleToScreen(this.ClientRectangle);
            petRect.Inflate(4, 4);
            bool onPet = petRect.Contains(mp);

            if (onPet && !wasOnPet)
            {
                hoverEnterTime = DateTime.Now;
            }

            if (!onPet)
            {
                hoverEnterTime = DateTime.MinValue;
                if (menuOn)
                {
                    bool onMenu = false, onBubble = false;
                    if (hoverMenu != null && !hoverMenu.IsDisposed)
                    {
                        Rectangle mr = new Rectangle(hoverMenu.PointToScreen(Point.Empty), hoverMenu.Size);
                        mr.Inflate(8, 8); onMenu = mr.Contains(mp);
                    }
                    if (panelBubble != null && !panelBubble.IsDisposed && panelBubble.Visible)
                    {
                        Rectangle br = new Rectangle(panelBubble.PointToScreen(Point.Empty), panelBubble.Size);
                        br.Inflate(8, 8); onBubble = br.Contains(mp);
                    }
                    if (!onMenu && !onBubble) HideHoverMenu();
                }
            }

            if (onPet && hoverEnterTime != DateTime.MinValue
                && (DateTime.Now - hoverEnterTime).TotalMilliseconds >= 200
                && !petMenu.Visible)
            {
                if (!menuOn) ShowHoverMenu();
            }

            wasOnPet = onPet;
        }
        private void ShowHoverMenu()
        {
            if (hoverMenu == null || hoverMenu.IsDisposed) hoverMenu = new HoverMenuForm(this);
            hoverMenu.ResetFoodPanel();
            hoverMenu.RefreshState();
            Point pos = this.PointToScreen(new Point(0, this.Height));
            hoverMenu.Location = new Point(pos.X, pos.Y + 2); hoverMenu.Show();
            if (panelBubble == null || panelBubble.IsDisposed) panelBubble = new PanelBubble(this);
            Point bp = this.PointToScreen(new Point(this.Width + 8, this.Height / 2 - 70));
            panelBubble.Location = bp; panelBubble.Show();
        }
        private void HideHoverMenu() { if (hoverMenu != null && !hoverMenu.IsDisposed) hoverMenu.Hide(); if (panelBubble != null && !panelBubble.IsDisposed) panelBubble.Hide(); }
        public void RefreshHoverMenu() { if (hoverMenu != null && !hoverMenu.IsDisposed && hoverMenu.Visible) hoverMenu.RefreshState(); }
        private void OnMouseDown(object sender, MouseEventArgs e) { dragStart = new Point(e.X, e.Y); isDragging = false; HideHoverMenu(); }
        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (!isDragging) { if (Math.Abs(e.X - dragStart.X) < 5 && Math.Abs(e.Y - dragStart.Y) < 5) return; isDragging = true; HideHoverMenu(); }
                Point oldLoc = this.Location;
                this.Location = new Point(this.Location.X + e.X - dragStart.X, this.Location.Y + e.Y - dragStart.Y);
                if (activeBubble != null && !activeBubble.IsDisposed)
                {
                    int dx = this.Location.X - oldLoc.X;
                    int dy = this.Location.Y - oldLoc.Y;
                    activeBubble.Location = new Point(activeBubble.Location.X + dx, activeBubble.Location.Y + dy);
                }
                if (panelBubble != null && !panelBubble.IsDisposed && panelBubble.Visible)
                {
                    int dx = this.Location.X - oldLoc.X;
                    int dy = this.Location.Y - oldLoc.Y;
                    panelBubble.Location = new Point(panelBubble.Location.X + dx, panelBubble.Location.Y + dy);
                }
            }
        }

        static void CheckForUpdate()
        {
            System.Threading.Thread t = new System.Threading.Thread(delegate()
            {
                try
                {
                    HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(VersionUrl + "?t=" + DateTime.Now.Ticks);
                    req.UserAgent = "DeskPet-Updater";
                    req.Timeout = 5000;
                    req.ReadWriteTimeout = 5000;
                    using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                    using (System.IO.StreamReader sr = new System.IO.StreamReader(resp.GetResponseStream()))
                    {
                        string remoteVersion = sr.ReadToEnd().Trim();
                        if (string.IsNullOrEmpty(remoteVersion)) return;
                        if (remoteVersion == CurrentVersion) return;

                        DialogResult result = MessageBox.Show(
                            "发现新版本 v" + remoteVersion + "（当前 v" + CurrentVersion + "）\n\n是否立即下载更新？",
                            "DeskPet 更新", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                        if (result != DialogResult.Yes) return;

                        string exePath = Application.ExecutablePath;
                        string newExePath = exePath + ".new";
                        string batPath = Path.Combine(Application.StartupPath, "update.bat");

                        Form progressForm = null;
                        ProgressBar progress = null;
                        Label progressLabel = null;
                        Label percentLabel = null;
                        try
                        {
                            progressForm = new Form();
                            progressForm.Text = "DeskPet 更新";
                            progressForm.Size = new Size(360, 130);
                            progressForm.StartPosition = FormStartPosition.CenterScreen;
                            progressForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                            progressForm.MaximizeBox = false;
                            progressForm.ControlBox = false;
                            progressForm.BackColor = Color.FromArgb(255, 252, 245);

                            progressLabel = new Label();
                            progressLabel.Text = "正在下载新版本...";
                            progressLabel.Location = new Point(20, 15);
                            progressLabel.Size = new Size(200, 20);
                            progressLabel.Font = new Font("Microsoft YaHei", 9);
                            progressForm.Controls.Add(progressLabel);

                            percentLabel = new Label();
                            percentLabel.Text = "0%";
                            percentLabel.Location = new Point(300, 15);
                            percentLabel.Size = new Size(40, 20);
                            percentLabel.Font = new Font("Microsoft YaHei", 9);
                            percentLabel.TextAlign = ContentAlignment.MiddleRight;
                            progressForm.Controls.Add(percentLabel);

                            progress = new ProgressBar();
                            progress.Location = new Point(20, 45);
                            progress.Size = new Size(320, 25);
                            progressForm.Controls.Add(progress);

                            Label hintLabel = new Label();
                            hintLabel.Text = "下载完成后将自动替换并重启";
                            hintLabel.Location = new Point(20, 78);
                            hintLabel.Size = new Size(320, 20);
                            hintLabel.Font = new Font("Microsoft YaHei", 8);
                            hintLabel.ForeColor = Color.Gray;
                            hintLabel.TextAlign = ContentAlignment.MiddleCenter;
                            progressForm.Controls.Add(hintLabel);

                            progressForm.Show();
                            progressForm.Refresh();

                            using (WebClient wc = new WebClient())
                            {
                                wc.Headers.Add("User-Agent", "DeskPet-Updater");
                                wc.DownloadProgressChanged += delegate(object sender, DownloadProgressChangedEventArgs e)
                                {
                                    try
                                    {
                                        progress.Maximum = 100;
                                        progress.Value = e.ProgressPercentage;
                                        percentLabel.Text = e.ProgressPercentage + "%";
                                        progressLabel.Text = "正在下载... " + (e.BytesReceived / 1024) + " KB / " + (e.TotalBytesToReceive / 1024) + " KB";
                                        progressForm.Refresh();
                                    }
                                    catch { }
                                };
                                wc.DownloadFileCompleted += delegate(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
                                {
                                    try
                                    {
                                        if (e.Error != null)
                                        {
                                            MessageBox.Show("下载失败: " + e.Error.Message, "更新失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                            progressForm.Close();
                                            return;
                                        }
                                        progressLabel.Text = "下载完成，正在安装...";
                                        progressForm.Refresh();

                                        string batContent = "@echo off\r\n";
                                        batContent += "echo 正在更新 DeskPet...\r\n";
                                        batContent += ":retry\r\n";
                                        batContent += "timeout /t 1 /nobreak >nul\r\n";
                                        batContent += "del /f \"" + exePath + "\" 2>nul\r\n";
                                        batContent += "if exist \"" + exePath + "\" goto retry\r\n";
                                        batContent += "move /y \"" + newExePath + "\" \"" + exePath + "\"\r\n";
                                        batContent += "start \"\" \"" + exePath + "\"\r\n";
                                        batContent += "del /f \"%~f0\" & exit\r\n";
                                        File.WriteAllText(batPath, batContent, System.Text.Encoding.Default);

                                        Process.Start(new ProcessStartInfo
                                        {
                                            FileName = batPath,
                                            WindowStyle = ProcessWindowStyle.Hidden,
                                            UseShellExecute = true
                                        });

                                        Environment.Exit(0);
                                    }
                                    catch (Exception ex2)
                                    {
                                        MessageBox.Show("更新失败: " + ex2.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        progressForm.Close();
                                    }
                                };
                                wc.DownloadFileAsync(new Uri(DownloadUrl), newExePath);

                                while (progressForm != null && !progressForm.IsDisposed)
                                {
                                    Application.DoEvents();
                                    System.Threading.Thread.Sleep(50);
                                }
                            }
                        }
                        catch (Exception ex2)
                        {
                            if (progressForm != null) progressForm.Close();
                            throw ex2;
                        }
                        return;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Update check failed: " + ex.Message);
                }
            });
            t.IsBackground = true;
            t.Start();
        }

        [STAThread]
        static void Main()
        {
            bool createdNew;
            using (System.Threading.Mutex mutex = new System.Threading.Mutex(true, "DeskPet_SingleInstance_2024", out createdNew))
            {
                if (!createdNew) { MessageBox.Show("桌面宠物已经在运行中！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
                CheckForUpdate();
                Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false); Application.Run(new PetForm());
            }
        }
    }

    // ==================== 宠物面板界面（增强版）====================
    public class PetDetailsForm : Form
    {
        private PetForm pet;
        private PictureBox petPic;
        private Label petNameLabel, intimacyLabel, levelLabel, expLabel, moodLabel, skinLabel;
        private ProgressBar expBar;
        private Button handBtn, feedBtn;
        private Label handCdLabel, feedCdLabel;
        private Timer animTimer; private int animStep; private Point petPicOriginal; private bool animClap;
        private Panel foodPanel;
        private bool showingFoodPanel = false;

        public PetDetailsForm(PetForm owner)
        {
            this.pet = owner; this.Text = "宠物面板"; this.Size = new Size(360, 660);
            this.FormBorderStyle = FormBorderStyle.FixedDialog; this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen; this.BackColor = pet.GetCurrentSkinBgColor();
            this.Font = new Font("Microsoft YaHei", 9);
            try { this.Icon = new Icon(Path.Combine(Application.StartupPath, "app_icon.ico")); } catch { }

            Panel topBar = new Panel(); topBar.BackColor = pet.GetCurrentSkinPrimaryColor(); topBar.Location = new Point(0, 0); topBar.Size = new Size(360, 4); this.Controls.Add(topBar);

            Button menuBtn = new Button(); menuBtn.Text = "⋯"; menuBtn.Font = new Font("Microsoft YaHei", 14, FontStyle.Bold);
            menuBtn.FlatStyle = FlatStyle.Flat; menuBtn.FlatAppearance.BorderSize = 0; menuBtn.ForeColor = pet.GetCurrentSkinPrimaryColor();
            menuBtn.BackColor = Color.Transparent; menuBtn.Location = new Point(315, 8); menuBtn.Size = new Size(30, 30); menuBtn.Cursor = Cursors.Hand;
            ContextMenuStrip mp = new ContextMenuStrip(); mp.Items.Add("修改名称", null, (s, e) => ShowRenameDialog());
            mp.Items.Add("-", null, null); mp.Items.Add("查看统计", null, (s, e) => pet.OpenStats(s, e));
            mp.Items.Add("查看成就", null, (s, e) => pet.OpenAchievements(s, e));
            mp.Items.Add("任务系统", null, (s, e) => pet.OpenQuests(s, e));
            mp.Items.Add("-", null, null); mp.Items.Add("重新领养", null, (s, e) => ShowResetAdoptDialog());
            menuBtn.Click += (s, e) => mp.Show(menuBtn, new Point(0, menuBtn.Height)); this.Controls.Add(menuBtn);

            petPic = new PictureBox(); petPic.Size = new Size(130, 130); petPic.Location = new Point(115, 55);
            petPic.BackColor = Color.White; petPic.SizeMode = PictureBoxSizeMode.Zoom;
            string imgPath = Path.Combine(Application.StartupPath, "微信图片_20260711143702_6_2.jpg");
            try { using (FileStream fs = new FileStream(imgPath, FileMode.Open, FileAccess.Read)) { petPic.Image = Image.FromStream(fs); } } catch { petPic.BackColor = Color.Coral; }
            this.Controls.Add(petPic); petPicOriginal = petPic.Location;

            petNameLabel = new Label(); petNameLabel.Text = pet.PetName; petNameLabel.Font = new Font("Microsoft YaHei", 14, FontStyle.Bold);
            petNameLabel.ForeColor = Color.FromArgb(80, 50, 40); petNameLabel.Location = new Point(20, 210); petNameLabel.Size = new Size(320, 28); petNameLabel.TextAlign = ContentAlignment.MiddleCenter; this.Controls.Add(petNameLabel);

            levelLabel = new Label(); levelLabel.Font = new Font("Microsoft YaHei", 10, FontStyle.Bold); levelLabel.ForeColor = Color.White;
            levelLabel.BackColor = pet.GetCurrentSkinPrimaryColor(); levelLabel.Location = new Point(110, 243); levelLabel.Size = new Size(140, 24); levelLabel.TextAlign = ContentAlignment.MiddleCenter; this.Controls.Add(levelLabel);

            moodLabel = new Label(); moodLabel.Font = new Font("Microsoft YaHei", 9); moodLabel.ForeColor = Color.FromArgb(120, 100, 90);
            moodLabel.Location = new Point(20, 272); moodLabel.Size = new Size(320, 20); moodLabel.TextAlign = ContentAlignment.MiddleCenter; this.Controls.Add(moodLabel);

            intimacyLabel = new Label(); intimacyLabel.Font = new Font("Microsoft YaHei", 10); intimacyLabel.ForeColor = Color.FromArgb(150, 120, 110);
            intimacyLabel.Location = new Point(20, 295); intimacyLabel.Size = new Size(320, 22); intimacyLabel.TextAlign = ContentAlignment.MiddleCenter; this.Controls.Add(intimacyLabel);

            Panel expPanel = new Panel(); expPanel.BackColor = Color.White; expPanel.Location = new Point(30, 325); expPanel.Size = new Size(300, 50); this.Controls.Add(expPanel);
            expBar = new ProgressBar(); expBar.Location = new Point(10, 8); expBar.Size = new Size(280, 18); expBar.Style = ProgressBarStyle.Continuous; expPanel.Controls.Add(expBar);
            expLabel = new Label(); expLabel.Location = new Point(10, 28); expLabel.Size = new Size(280, 18); expLabel.TextAlign = ContentAlignment.MiddleCenter;
            expLabel.Font = new Font("Microsoft YaHei", 8); expLabel.ForeColor = Color.FromArgb(160, 140, 130); expPanel.Controls.Add(expLabel);

            handBtn = new Button(); handBtn.Text = "握 手"; handBtn.Location = new Point(30, 430); handBtn.Size = new Size(145, 45);
            handBtn.BackColor = pet.GetCurrentSkinPrimaryColor(); handBtn.ForeColor = Color.White; handBtn.FlatStyle = FlatStyle.Flat;
            handBtn.FlatAppearance.BorderSize = 0; handBtn.Font = new Font("Microsoft YaHei", 10, FontStyle.Bold); handBtn.Cursor = Cursors.Hand;
            handBtn.Click += (s, e) => { if (pet.CanHandshake()) { pet.DoHandshake(); StartClapAnim(); } RefreshUI(); }; this.Controls.Add(handBtn);

            feedBtn = new Button(); feedBtn.Text = "喂 食"; feedBtn.Location = new Point(185, 430); feedBtn.Size = new Size(145, 45);
            feedBtn.BackColor = pet.GetCurrentSkinSecondaryColor(); feedBtn.ForeColor = Color.White; feedBtn.FlatStyle = FlatStyle.Flat;
            feedBtn.FlatAppearance.BorderSize = 0; feedBtn.Font = new Font("Microsoft YaHei", 10, FontStyle.Bold); feedBtn.Cursor = Cursors.Hand;
            feedBtn.Click += (s, e) => { if (pet.CanFeed()) { ShowFoodPanel(); } }; this.Controls.Add(feedBtn);

            handCdLabel = new Label(); handCdLabel.Location = new Point(30, 478); handCdLabel.Size = new Size(145, 20); handCdLabel.TextAlign = ContentAlignment.MiddleCenter;
            handCdLabel.Font = new Font("Microsoft YaHei", 8); handCdLabel.ForeColor = Color.FromArgb(180, 150, 140); this.Controls.Add(handCdLabel);
            feedCdLabel = new Label(); feedCdLabel.Location = new Point(185, 478); feedCdLabel.Size = new Size(145, 20); feedCdLabel.TextAlign = ContentAlignment.MiddleCenter;
            feedCdLabel.Font = new Font("Microsoft YaHei", 8); feedCdLabel.ForeColor = Color.FromArgb(180, 150, 140); this.Controls.Add(feedCdLabel);

            // 任务系统入口按钮（底部居中）
            Button questBtn = new Button();
            questBtn.Text = "任务  \uD83D\uDCCB";
            questBtn.Font = new Font("Microsoft YaHei", 9, FontStyle.Bold);
            questBtn.FlatStyle = FlatStyle.Flat;
            questBtn.FlatAppearance.BorderSize = 0;
            questBtn.BackColor = pet.GetCurrentSkinPrimaryColor();
            questBtn.ForeColor = Color.White;
            questBtn.Location = new Point(30, 508);
            questBtn.Size = new Size(95, 38);
            questBtn.Cursor = Cursors.Hand;
            questBtn.Click += (s, e) => { pet.OpenQuests(s, e); };
            this.Controls.Add(questBtn);

            // 小游戏入口按钮
            Button gameBtn = new Button();
            gameBtn.Text = "小游戏";
            gameBtn.Font = new Font("Microsoft YaHei", 9, FontStyle.Bold);
            gameBtn.FlatStyle = FlatStyle.Flat;
            gameBtn.FlatAppearance.BorderSize = 0;
            gameBtn.BackColor = Color.FromArgb(120, 180, 120);
            gameBtn.ForeColor = Color.White;
            gameBtn.Location = new Point(132, 508);
            gameBtn.Size = new Size(95, 38);
            gameBtn.Cursor = Cursors.Hand;
            gameBtn.Click += (s, e) => { pet.OpenGameSelect(); };
            this.Controls.Add(gameBtn);

            // 商城系统入口按钮
            Button shopBtn = new Button();
            shopBtn.Text = "商城  \uD83D\uDED2";
            shopBtn.Font = new Font("Microsoft YaHei", 9, FontStyle.Bold);
            shopBtn.FlatStyle = FlatStyle.Flat;
            shopBtn.FlatAppearance.BorderSize = 0;
            shopBtn.BackColor = pet.GetCurrentSkinSecondaryColor();
            shopBtn.ForeColor = Color.White;
            shopBtn.Location = new Point(234, 508);
            shopBtn.Size = new Size(95, 38);
            shopBtn.Cursor = Cursors.Hand;
            shopBtn.Click += (s, e) => { pet.OpenShop(); };
            this.Controls.Add(shopBtn);

            skinLabel = new Label(); skinLabel.Font = new Font("Microsoft YaHei", 8); skinLabel.ForeColor = Color.FromArgb(180, 150, 140);
            skinLabel.Location = new Point(20, 552); skinLabel.Size = new Size(320, 18); skinLabel.TextAlign = ContentAlignment.MiddleCenter; this.Controls.Add(skinLabel);

            Label tipLabel = new Label(); tipLabel.Text = "双击宠物可以摸头哦~  右键切换皮肤 ✨"; tipLabel.Location = new Point(20, 580);
            tipLabel.Size = new Size(320, 22); tipLabel.TextAlign = ContentAlignment.MiddleCenter; tipLabel.Font = new Font("Microsoft YaHei", 8);
            tipLabel.ForeColor = Color.FromArgb(200, 170, 160); this.Controls.Add(tipLabel);

            Label dayNightTip = new Label(); dayNightTip.Text = pet.IsNightMode ? "🌙 夜间模式" : "☀️ 日间模式"; dayNightTip.Location = new Point(20, 604);
            dayNightTip.Size = new Size(320, 18); dayNightTip.TextAlign = ContentAlignment.MiddleCenter; dayNightTip.Font = new Font("Microsoft YaHei", 8);
            dayNightTip.ForeColor = Color.FromArgb(180, 160, 150); this.Controls.Add(dayNightTip);

            animTimer = new Timer(); animTimer.Interval = 60; animTimer.Tick += AnimTimer_Tick;
            this.FormClosing += (s, e) => { this.Hide(); e.Cancel = true; };

            // 食物选择面板（初始隐藏）
            foodPanel = new Panel();
            foodPanel.Location = new Point(10, 385);
            foodPanel.Size = new Size(338, 240);
            foodPanel.BackColor = Color.FromArgb(255, 252, 245);
            foodPanel.BorderStyle = BorderStyle.FixedSingle;
            foodPanel.Visible = false;
            this.Controls.Add(foodPanel);

            RefreshUI();
        }

        private void ShowFoodPanel()
        {
            showingFoodPanel = true;
            handBtn.Visible = false; feedBtn.Visible = false;
            handCdLabel.Visible = false; feedCdLabel.Visible = false;
            // 隐藏底部所有控件
            foreach (Control c in this.Controls)
            {
                if (c.Location.Y >= 500 && c != foodPanel) c.Visible = false;
            }
            BuildFoodPanel();
            foodPanel.Visible = true;
        }

        private void HideFoodPanel()
        {
            showingFoodPanel = false;
            foodPanel.Visible = false;
            foodPanel.Controls.Clear();
            handBtn.Visible = true; feedBtn.Visible = true;
            handCdLabel.Visible = true; feedCdLabel.Visible = true;
            foreach (Control c in this.Controls)
            {
                if (c.Location.Y >= 500 && c != foodPanel && c != handBtn && c != feedBtn && c != handCdLabel && c != feedCdLabel) c.Visible = true;
            }
            RefreshUI();
        }

        private void BuildFoodPanel()
        {
            foodPanel.Controls.Clear();
            Label title = new Label();
            title.Text = "🍽️ 选择食物  💎" + pet.GetCrystals();
            title.Font = new Font("Microsoft YaHei", 10, FontStyle.Bold);
            title.ForeColor = Color.FromArgb(80, 50, 40);
            title.Location = new Point(8, 4);
            title.Size = new Size(280, 24);
            foodPanel.Controls.Add(title);

            Button backBtn = new Button();
            backBtn.Text = "✕";
            backBtn.FlatStyle = FlatStyle.Flat;
            backBtn.FlatAppearance.BorderSize = 0;
            backBtn.BackColor = Color.FromArgb(210, 200, 195);
            backBtn.ForeColor = Color.White;
            backBtn.Font = new Font("Microsoft YaHei", 9, FontStyle.Bold);
            backBtn.Location = new Point(300, 4);
            backBtn.Size = new Size(30, 24);
            backBtn.Cursor = Cursors.Hand;
            backBtn.Click += (s, e) => { HideFoodPanel(); };
            foodPanel.Controls.Add(backBtn);

            int y = 34;
            for (int i = 0; i < pet.foodDefs.Length; i++)
            {
                Panel card = CreateFoodCard(pet.foodDefs[i], y, pet.GetFoodStock(i), pet.foodMaxStock[i]);
                foodPanel.Controls.Add(card);
                y += 40;
            }
        }

        private Panel CreateFoodCard(PetForm.FoodDef food, int y, int stock, int maxStock)
        {
            Panel card = new Panel();
            card.Location = new Point(6, y);
            card.Size = new Size(324, 34);
            card.BackColor = Color.White;

            Label icon = new Label();
            icon.Text = food.icon;
            icon.Font = new Font("Segoe UI Emoji", 14);
            icon.Location = new Point(4, 4);
            icon.Size = new Size(28, 26);
            icon.TextAlign = ContentAlignment.MiddleCenter;
            card.Controls.Add(icon);

            Label name = new Label();
            name.Text = food.name;
            name.Font = new Font("Microsoft YaHei", 9, FontStyle.Bold);
            name.ForeColor = Color.FromArgb(60, 50, 40);
            name.Location = new Point(36, 2);
            name.Size = new Size(50, 16);
            card.Controls.Add(name);

            Label intimacy = new Label();
            intimacy.Text = "+" + food.intimacy;
            intimacy.Font = new Font("Microsoft YaHei", 7);
            intimacy.ForeColor = Color.FromArgb(100, 180, 120);
            intimacy.Location = new Point(36, 18);
            intimacy.Size = new Size(40, 14);
            card.Controls.Add(intimacy);

            Label price = new Label();
            if (food.price == 0)
            {
                price.Text = "免费";
                price.ForeColor = Color.FromArgb(100, 180, 100);
            }
            else
            {
                price.Text = "💎" + food.price;
                price.ForeColor = Color.FromArgb(80, 175, 210);
            }
            price.Font = new Font("Microsoft YaHei", 9, FontStyle.Bold);
            price.Location = new Point(80, 2);
            price.Size = new Size(45, 30);
            price.TextAlign = ContentAlignment.MiddleCenter;
            card.Controls.Add(price);

            if (food.price > 0)
            {
                Label stockLabel = new Label();
                stockLabel.Text = "x" + stock;
                stockLabel.Font = new Font("Microsoft YaHei", 6);
                stockLabel.ForeColor = stock <= 0 ? Color.FromArgb(220, 100, 80) : Color.FromArgb(140, 130, 120);
                stockLabel.Location = new Point(128, 2);
                stockLabel.Size = new Size(18, 30);
                stockLabel.TextAlign = ContentAlignment.MiddleCenter;
                card.Controls.Add(stockLabel);
            }

            Button buyBtn = new Button();
            buyBtn.Text = "喂这个";
            buyBtn.FlatStyle = FlatStyle.Flat;
            buyBtn.FlatAppearance.BorderSize = 0;
            buyBtn.Font = new Font("Microsoft YaHei", 8, FontStyle.Bold);
            buyBtn.Location = new Point(150, 2);
            buyBtn.Size = new Size(168, 30);
            buyBtn.Cursor = Cursors.Hand;

            bool cdReady = (DateTime.Now - pet.GetLastFeedTime()).TotalHours >= 4;
            bool canFeed = pet.Intimacy < 999 && cdReady && (stock > 0 || food.price == 0);

            if (canFeed)
            {
                buyBtn.BackColor = food.price == 0 ? Color.FromArgb(100, 190, 120) : pet.GetCurrentSkinSecondaryColor();
                buyBtn.ForeColor = Color.White;
            }
            else
            {
                buyBtn.BackColor = Color.FromArgb(210, 205, 200);
                buyBtn.ForeColor = Color.FromArgb(150, 145, 140);
                buyBtn.Enabled = false;
            }

            PetForm.FoodDef capturedFood = food;
            buyBtn.Click += (s, e) =>
            {
                if (pet.BuyFood(capturedFood))
                {
                    BuildFoodPanel();
                    RefreshUI();
                }
            };
            card.Controls.Add(buyBtn);

            return card;
        }

        private void ShowRenameDialog()
        {
            Form rf = new Form(); rf.Text = "修改名称"; rf.Size = new Size(280, 150); rf.FormBorderStyle = FormBorderStyle.FixedDialog;
            rf.MaximizeBox = false; rf.StartPosition = FormStartPosition.CenterParent; rf.BackColor = pet.GetCurrentSkinBgColor();
            Label l = new Label(); l.Text = "给宠物取个名字吧："; l.Font = new Font("Microsoft YaHei", 9); l.ForeColor = Color.FromArgb(80, 50, 40);
            l.Location = new Point(20, 20); l.Size = new Size(240, 22); rf.Controls.Add(l);
            TextBox txt = new TextBox(); txt.Text = pet.PetName; txt.Location = new Point(20, 50); txt.Size = new Size(160, 25); txt.Font = new Font("Microsoft YaHei", 10); rf.Controls.Add(txt);
            Button ok = new Button(); ok.Text = "确定"; ok.Location = new Point(190, 49); ok.Size = new Size(60, 28);
            ok.BackColor = pet.GetCurrentSkinPrimaryColor(); ok.ForeColor = Color.White; ok.FlatStyle = FlatStyle.Flat; ok.FlatAppearance.BorderSize = 0;
            ok.Font = new Font("Microsoft YaHei", 9);
            ok.Click += (s, ev) => { string n = txt.Text.Trim(); if (!string.IsNullOrEmpty(n)) { pet.PetName = n; pet.UpdatePetToolTip(); SavePetName(); RefreshUI(); pet.CheckAndUnlockAchievement("rename"); } rf.Close(); }; rf.Controls.Add(ok); rf.ShowDialog(this);
        }
        private void ShowResetAdoptDialog()
        {
            Form dlg = new Form();
            dlg.Text = "重新领养"; dlg.Size = new Size(340, 200); dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
            dlg.MaximizeBox = false; dlg.StartPosition = FormStartPosition.CenterParent; dlg.BackColor = Color.FromArgb(255, 248, 240);
            Label warn = new Label(); warn.Text = "⚠ 确定要重新领养吗？\n所有数据将无法恢复！"; warn.Font = new Font("Microsoft YaHei", 10);
            warn.ForeColor = Color.FromArgb(200, 80, 60); warn.Location = new Point(20, 20); warn.Size = new Size(300, 44); dlg.Controls.Add(warn);
            Button confirmBtn = new Button(); confirmBtn.Text = "确认领养 (3s)"; confirmBtn.Enabled = false;
            confirmBtn.Location = new Point(60, 90); confirmBtn.Size = new Size(100, 36);
            confirmBtn.BackColor = Color.FromArgb(220, 80, 60); confirmBtn.ForeColor = Color.White;
            confirmBtn.FlatStyle = FlatStyle.Flat; confirmBtn.FlatAppearance.BorderSize = 0;
            confirmBtn.Font = new Font("Microsoft YaHei", 9, FontStyle.Bold);
            confirmBtn.Click += (s, ev) => { dlg.Close(); pet.ResetAllData(); RefreshUI(); pet.UpdatePetToolTip(); };
            dlg.Controls.Add(confirmBtn);
            Button cancelBtn = new Button(); cancelBtn.Text = "取消"; cancelBtn.Location = new Point(180, 90); cancelBtn.Size = new Size(100, 36);
            cancelBtn.BackColor = Color.FromArgb(180, 180, 180); cancelBtn.ForeColor = Color.White;
            cancelBtn.FlatStyle = FlatStyle.Flat; cancelBtn.FlatAppearance.BorderSize = 0;
            cancelBtn.Font = new Font("Microsoft YaHei", 9); cancelBtn.Click += (s, ev) => { dlg.Close(); }; dlg.Controls.Add(cancelBtn);
            Timer countdown = new Timer(); countdown.Interval = 1000; int remaining = 3;
            countdown.Tick += (s, ev) => { remaining--; if (remaining <= 0) { countdown.Stop(); confirmBtn.Enabled = true; confirmBtn.Text = "确认领养"; confirmBtn.BackColor = Color.FromArgb(220, 80, 60); } else { confirmBtn.Text = "确认领养 (" + remaining + "s)"; } };
            countdown.Start();
            dlg.ShowDialog(this);
        }
        private void StartClapAnim() { animStep = 0; animClap = true; petPicOriginal = petPic.Location; animTimer.Start(); }
        private void StartBounceAnim() { animStep = 0; animClap = false; petPicOriginal = petPic.Location; animTimer.Start(); }
        private void AnimTimer_Tick(object sender, EventArgs e)
        {
            if (animClap) { animStep++; int off = (animStep / 2) % 2 == 0 ? 4 : -4; petPic.Location = new Point(petPicOriginal.X + off, petPicOriginal.Y); if (animStep >= 12) { animTimer.Stop(); petPic.Location = petPicOriginal; } }
            else { animStep++; int off = (animStep % 2 == 0) ? -6 : 6; petPic.Location = new Point(petPicOriginal.X, petPicOriginal.Y + off); if (animStep >= 10) { animTimer.Stop(); petPic.Location = petPicOriginal; } }
        }
        public void RefreshUI()
        {
            petNameLabel.Text = pet.PetName; int lv = int.Parse(pet.GetLevel()); int exp = pet.GetLevelExp(); int mx = pet.GetLevelMax();
            intimacyLabel.Text = "亲密度 " + pet.Intimacy; levelLabel.Text = "Lv." + lv + "  " + PetForm.GetLevelTitle(lv);
            moodLabel.Text = "心情：" + pet.GetCurrentMoodEmoji() + " " + pet.GetCurrentMoodDescription();
            if (lv >= 4) levelLabel.BackColor = Color.FromArgb(255, 80, 120); else if (lv >= 3) levelLabel.BackColor = Color.FromArgb(255, 170, 60); else if (lv >= 2) levelLabel.BackColor = pet.GetCurrentSkinPrimaryColor(); else levelLabel.BackColor = Color.FromArgb(180, 200, 140);
            int pct = mx > 0 ? (int)((double)exp / mx * 100) : 100; if (pct > 100) pct = 100; expBar.Maximum = 100; expBar.Value = pct; expLabel.Text = "经验 " + exp + " / " + mx + "  (" + pct + "%)";
            if (lv >= 4) expBar.ForeColor = Color.FromArgb(255, 80, 120); else if (lv >= 3) expBar.ForeColor = Color.FromArgb(255, 170, 60); else if (lv >= 2) expBar.ForeColor = pet.GetCurrentSkinPrimaryColor(); else expBar.ForeColor = Color.FromArgb(140, 190, 120);
            bool ch = pet.CanHandshake(), cf = pet.CanFeed(); handBtn.Enabled = ch; feedBtn.Enabled = cf;
            handBtn.BackColor = ch ? pet.GetCurrentSkinPrimaryColor() : Color.FromArgb(210, 200, 195);
            feedBtn.BackColor = cf ? pet.GetCurrentSkinSecondaryColor() : Color.FromArgb(210, 200, 195);
            string hcd = pet.GetHandCdRemaining(); string fcd = pet.GetFeedCdRemaining();
            handCdLabel.Text = ch ? "可互动" : (hcd == "亲密度已满" ? hcd : hcd + "后可再次握手");
            feedCdLabel.Text = cf ? "可互动" : (fcd == "亲密度已满" ? fcd : fcd + "后可再次喂食");
            skinLabel.Text = "当前皮肤：" + pet.GetCurrentSkinName() + (pet.IsNightMode ? "  |  🌙夜间" : "  |  ☀️日间");
        }
        private void SavePetName() { try { File.WriteAllText(Path.Combine(Application.StartupPath, "pet_name.txt"), pet.PetName); } catch { } }
    }

    // ==================== 互动统计界面 ====================
    public class StatsForm : Form
    {
        private PetForm pet;
        public StatsForm(PetForm owner)
        {
            this.pet = owner; this.Text = "互动统计"; this.Size = new Size(340, 450);
            this.FormBorderStyle = FormBorderStyle.FixedDialog; this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen; this.BackColor = pet.GetCurrentSkinBgColor();
            try { this.Icon = new Icon(Path.Combine(Application.StartupPath, "app_icon.ico")); } catch { }
            Panel topBar = new Panel(); topBar.BackColor = pet.GetCurrentSkinPrimaryColor(); topBar.Location = new Point(0, 0); topBar.Size = new Size(340, 4); this.Controls.Add(topBar);
            Label tl = new Label(); tl.Text = "📊 互动数据统计"; tl.Font = new Font("Microsoft YaHei", 12, FontStyle.Bold); tl.ForeColor = Color.FromArgb(80, 50, 40); tl.Location = new Point(20, 15); tl.Size = new Size(300, 28); tl.TextAlign = ContentAlignment.MiddleCenter; this.Controls.Add(tl);
            CreateStatRow("🤝 累计握手", pet.TotalHandshakes.ToString() + " 次", 60);
            CreateStatRow("🍖 累计喂食", pet.TotalFeeds.ToString() + " 次", 100);
            CreateStatRow("✋ 累计摸头", pet.TotalPats.ToString() + " 次", 140);
            CreateStatRow("❤️ 当前亲密度", pet.Intimacy.ToString() + " / 999", 180);
            CreateStatRow("⭐ 当前等级", "Lv." + pet.GetLevel() + " " + PetForm.GetLevelTitle(int.Parse(pet.GetLevel())), 220);
            CreateStatRow("📅 相处天数", pet.TotalDaysActive.ToString() + " 天", 260);
            CreateStatRow("🏆 已解锁成就", pet.UnlockedAchievements.Count.ToString() + " / " + pet.AllAchievements.Length, 300);
            CreateStatRow("😊 当前心情", pet.GetCurrentMoodEmoji() + " " + pet.GetCurrentMoodDescription(), 340);
            Button cb = new Button(); cb.Text = "关闭"; cb.Location = new Point(120, 380); cb.Size = new Size(100, 32); cb.BackColor = pet.GetCurrentSkinPrimaryColor(); cb.ForeColor = Color.White; cb.FlatStyle = FlatStyle.Flat; cb.FlatAppearance.BorderSize = 0; cb.Font = new Font("Microsoft YaHei", 9); cb.Click += (s, e) => { this.Hide(); }; this.Controls.Add(cb);
            this.FormClosing += (s, e) => { this.Hide(); e.Cancel = true; };
        }
        public void RefreshData()
        {
            this.Controls.Clear();
            Panel topBar = new Panel(); topBar.BackColor = pet.GetCurrentSkinPrimaryColor(); topBar.Location = new Point(0, 0); topBar.Size = new Size(340, 4); this.Controls.Add(topBar);
            Label tl = new Label(); tl.Text = "📊 互动数据统计"; tl.Font = new Font("Microsoft YaHei", 12, FontStyle.Bold); tl.ForeColor = Color.FromArgb(80, 50, 40); tl.Location = new Point(20, 15); tl.Size = new Size(300, 28); tl.TextAlign = ContentAlignment.MiddleCenter; this.Controls.Add(tl);
            CreateStatRow("🤝 累计握手", pet.TotalHandshakes.ToString() + " 次", 60);
            CreateStatRow("🍖 累计喂食", pet.TotalFeeds.ToString() + " 次", 100);
            CreateStatRow("✋ 累计摸头", pet.TotalPats.ToString() + " 次", 140);
            CreateStatRow("❤️ 当前亲密度", pet.Intimacy.ToString() + " / 999", 180);
            CreateStatRow("⭐ 当前等级", "Lv." + pet.GetLevel() + " " + PetForm.GetLevelTitle(int.Parse(pet.GetLevel())), 220);
            CreateStatRow("📅 相处天数", pet.TotalDaysActive.ToString() + " 天", 260);
            CreateStatRow("🏆 已解锁成就", pet.UnlockedAchievements.Count.ToString() + " / " + pet.AllAchievements.Length, 300);
            CreateStatRow("😊 当前心情", pet.GetCurrentMoodEmoji() + " " + pet.GetCurrentMoodDescription(), 340);
            Button cb = new Button(); cb.Text = "关闭"; cb.Location = new Point(120, 380); cb.Size = new Size(100, 32); cb.BackColor = pet.GetCurrentSkinPrimaryColor(); cb.ForeColor = Color.White; cb.FlatStyle = FlatStyle.Flat; cb.FlatAppearance.BorderSize = 0; cb.Font = new Font("Microsoft YaHei", 9); cb.Click += (s, e) => { this.Hide(); }; this.Controls.Add(cb);
        }
        private void CreateStatRow(string label, string value, int y)
        {
            Panel row = new Panel(); row.Location = new Point(20, y); row.Size = new Size(300, 34); row.BackColor = Color.White; this.Controls.Add(row);
            Label lb = new Label(); lb.Text = label; lb.Font = new Font("Microsoft YaHei", 9); lb.ForeColor = Color.FromArgb(100, 80, 70); lb.Location = new Point(10, 7); lb.Size = new Size(160, 20); row.Controls.Add(lb);
            Label vl = new Label(); vl.Text = value; vl.Font = new Font("Microsoft YaHei", 9, FontStyle.Bold); vl.ForeColor = pet.GetCurrentSkinPrimaryColor(); vl.Location = new Point(180, 7); vl.Size = new Size(110, 20); vl.TextAlign = ContentAlignment.MiddleRight; row.Controls.Add(vl);
        }
    }

    // ==================== 成就墙界面 ====================
    public class AchievementForm : Form
    {
        private PetForm pet; private FlowLayoutPanel achievementPanel;
        public AchievementForm(PetForm owner)
        {
            this.pet = owner; this.Text = "成就墙"; this.Size = new Size(380, 550);
            this.FormBorderStyle = FormBorderStyle.FixedDialog; this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen; this.BackColor = pet.GetCurrentSkinBgColor();
            try { this.Icon = new Icon(Path.Combine(Application.StartupPath, "app_icon.ico")); } catch { }
            Panel topBar = new Panel(); topBar.BackColor = pet.GetCurrentSkinPrimaryColor(); topBar.Location = new Point(0, 0); topBar.Size = new Size(380, 4); this.Controls.Add(topBar);
            Label tl = new Label(); tl.Text = "🏆 成就墙  (" + pet.UnlockedAchievements.Count + "/" + pet.AllAchievements.Length + ")";
            tl.Font = new Font("Microsoft YaHei", 12, FontStyle.Bold); tl.ForeColor = Color.FromArgb(80, 50, 40); tl.Location = new Point(20, 12); tl.Size = new Size(340, 28); tl.TextAlign = ContentAlignment.MiddleCenter; this.Controls.Add(tl);
            achievementPanel = new FlowLayoutPanel(); achievementPanel.Location = new Point(15, 48); achievementPanel.Size = new Size(350, 420);
            achievementPanel.FlowDirection = FlowDirection.LeftToRight; achievementPanel.WrapContents = true; achievementPanel.AutoScroll = true; achievementPanel.BackColor = Color.Transparent; this.Controls.Add(achievementPanel);
            RefreshList();
            Button cb = new Button(); cb.Text = "关闭"; cb.Location = new Point(140, 485); cb.Size = new Size(100, 30); cb.BackColor = pet.GetCurrentSkinPrimaryColor(); cb.ForeColor = Color.White; cb.FlatStyle = FlatStyle.Flat; cb.FlatAppearance.BorderSize = 0; cb.Font = new Font("Microsoft YaHei", 9); cb.Click += (s, e) => { this.Hide(); }; this.Controls.Add(cb);
            this.FormClosing += (s, e) => { this.Hide(); e.Cancel = true; };
        }
        public void RefreshList()
        {
            achievementPanel.Controls.Clear();
            foreach (var ach in pet.AllAchievements)
            {
                bool unlocked = pet.UnlockedAchievements.Contains(ach.id);
                Panel card = new Panel(); card.Size = new Size(165, 72); card.BackColor = unlocked ? Color.FromArgb(255, 252, 245) : Color.FromArgb(245, 242, 238); card.BorderStyle = BorderStyle.FixedSingle;
                Label ic = new Label(); ic.Text = ach.icon; ic.Font = new Font("Segoe UI Emoji", 14); ic.Location = new Point(4, 4); ic.Size = new Size(24, 24); ic.TextAlign = ContentAlignment.MiddleCenter; card.Controls.Add(ic);
                Label nm = new Label(); nm.Text = ach.name; nm.Font = new Font("Microsoft YaHei", 8, FontStyle.Bold); nm.ForeColor = unlocked ? Color.FromArgb(180, 130, 50) : Color.FromArgb(160, 155, 150); nm.Location = new Point(32, 4); nm.Size = new Size(127, 18); card.Controls.Add(nm);
                Label dc = new Label(); dc.Text = ach.desc; dc.Font = new Font("Microsoft YaHei", 7); dc.ForeColor = unlocked ? Color.FromArgb(130, 110, 90) : Color.FromArgb(170, 165, 160); dc.Location = new Point(8, 28); dc.Size = new Size(149, 18); card.Controls.Add(dc);
                Label st = new Label(); st.Text = unlocked ? "✅ 已解锁" : "🔒 未解锁"; st.Font = new Font("Microsoft YaHei", 7, FontStyle.Bold); st.ForeColor = unlocked ? Color.FromArgb(100, 170, 100) : Color.FromArgb(170, 170, 170); st.Location = new Point(8, 48); st.Size = new Size(149, 16); card.Controls.Add(st);
                if (!unlocked) ic.ForeColor = Color.FromArgb(180, 180, 180);
                achievementPanel.Controls.Add(card);
            }
        }
    }

    // ==================== 悬停快捷菜单 ====================
    public class HoverMenuForm : Form
    {
        private PetForm pet; private Button handBtn, feedBtn; private Label handCdLabel, feedCdLabel;
        private Panel border, inner;
        private bool showingFoodPanel = false;
        private int baseHeight = 85;
        public HoverMenuForm(PetForm owner)
        {
            this.pet = owner; this.FormBorderStyle = FormBorderStyle.None; this.TopMost = true; this.Size = new Size(140, baseHeight);
            this.BackColor = Color.White; this.ShowInTaskbar = false; this.StartPosition = FormStartPosition.Manual;
            border = new Panel(); border.BackColor = Color.FromArgb(240, 225, 218); border.Location = new Point(0, 0); border.Size = new Size(140, baseHeight); this.Controls.Add(border);
            inner = new Panel(); inner.BackColor = Color.White; inner.Location = new Point(2, 2); inner.Size = new Size(136, 81); border.Controls.Add(inner);
            handBtn = new Button(); handBtn.Text = "握手"; handBtn.Location = new Point(5, 5); handBtn.Size = new Size(60, 42);
            handBtn.BackColor = pet.GetCurrentSkinPrimaryColor(); handBtn.ForeColor = Color.White; handBtn.FlatStyle = FlatStyle.Flat; handBtn.FlatAppearance.BorderSize = 0;
            handBtn.Font = new Font("Microsoft YaHei", 9, FontStyle.Bold); handBtn.Cursor = Cursors.Hand;
            handBtn.Click += (s, e) => { if (pet.CanHandshake()) { pet.DoHandshake(); RefreshState(); } }; inner.Controls.Add(handBtn);
            feedBtn = new Button(); feedBtn.Text = "喂食"; feedBtn.Location = new Point(71, 5); feedBtn.Size = new Size(60, 42);
            feedBtn.BackColor = pet.GetCurrentSkinSecondaryColor(); feedBtn.ForeColor = Color.White; feedBtn.FlatStyle = FlatStyle.Flat; feedBtn.FlatAppearance.BorderSize = 0;
            feedBtn.Font = new Font("Microsoft YaHei", 9, FontStyle.Bold); feedBtn.Cursor = Cursors.Hand;
            feedBtn.Click += (s, e) => { if (pet.CanFeed()) { ShowFoodPanel(); } }; inner.Controls.Add(feedBtn);
            handCdLabel = new Label(); handCdLabel.Location = new Point(5, 50); handCdLabel.Size = new Size(60, 16); handCdLabel.TextAlign = ContentAlignment.MiddleCenter; handCdLabel.Font = new Font("Microsoft YaHei", 7); handCdLabel.ForeColor = Color.FromArgb(160, 140, 130); inner.Controls.Add(handCdLabel);
            feedCdLabel = new Label(); feedCdLabel.Location = new Point(71, 50); feedCdLabel.Size = new Size(60, 16); feedCdLabel.TextAlign = ContentAlignment.MiddleCenter; feedCdLabel.Font = new Font("Microsoft YaHei", 7); feedCdLabel.ForeColor = Color.FromArgb(160, 140, 130); inner.Controls.Add(feedCdLabel);
            RefreshState();
        }
        private void ShowFoodPanel()
        {
            if (showingFoodPanel) return;
            showingFoodPanel = true;
            int foodCount = pet.foodDefs.Length;
            int expandedHeight = baseHeight + foodCount * 38 + 32;
            this.Size = new Size(140, expandedHeight);
            border.Size = new Size(140, expandedHeight);

            Label title = new Label();
            title.Text = "食物 💎" + pet.GetCrystals();
            title.Font = new Font("Microsoft YaHei", 8, FontStyle.Bold);
            title.ForeColor = Color.FromArgb(80, 50, 40);
            title.Location = new Point(4, 86);
            title.Size = new Size(100, 16);
            border.Controls.Add(title);

            Button closeBtn = new Button();
            closeBtn.Text = "✕";
            closeBtn.FlatStyle = FlatStyle.Flat;
            closeBtn.FlatAppearance.BorderSize = 0;
            closeBtn.BackColor = Color.FromArgb(210, 200, 195);
            closeBtn.ForeColor = Color.White;
            closeBtn.Font = new Font("Microsoft YaHei", 7, FontStyle.Bold);
            closeBtn.Location = new Point(112, 86);
            closeBtn.Size = new Size(22, 16);
            closeBtn.Cursor = Cursors.Hand;
            closeBtn.Click += (s, e) => { HideFoodPanel(); };
            border.Controls.Add(closeBtn);

            int y = 106;
            for (int i = 0; i < pet.foodDefs.Length; i++)
            {
                Panel card = CreateFoodCard(pet.foodDefs[i], y, pet.GetFoodStock(i));
                border.Controls.Add(card);
                y += 38;
            }
        }
        private void HideFoodPanel()
        {
            if (!showingFoodPanel) return;
            showingFoodPanel = false;
            // 移除食物面板控件（保留border和inner）
            for (int i = border.Controls.Count - 1; i >= 0; i--)
            {
                Control c = border.Controls[i];
                if (c != inner) { border.Controls.Remove(c); c.Dispose(); }
            }
            this.Size = new Size(140, baseHeight);
            border.Size = new Size(140, baseHeight);
        }
        private Panel CreateFoodCard(PetForm.FoodDef food, int y, int stock)
        {
            Panel card = new Panel();
            card.Location = new Point(4, y);
            card.Size = new Size(132, 32);
            card.BackColor = Color.White;

            Label icon = new Label();
            icon.Text = food.icon;
            icon.Font = new Font("Segoe UI Emoji", 12);
            icon.Location = new Point(2, 4);
            icon.Size = new Size(22, 24);
            icon.TextAlign = ContentAlignment.MiddleCenter;
            card.Controls.Add(icon);

            Label name = new Label();
            name.Text = food.name;
            name.Font = new Font("Microsoft YaHei", 7, FontStyle.Bold);
            name.ForeColor = Color.FromArgb(60, 50, 40);
            name.Location = new Point(24, 1);
            name.Size = new Size(40, 14);
            card.Controls.Add(name);

            Label price = new Label();
            if (food.price == 0)
            {
                price.Text = "免费";
                price.ForeColor = Color.FromArgb(100, 180, 100);
            }
            else
            {
                price.Text = "💎" + food.price;
                price.ForeColor = Color.FromArgb(80, 175, 210);
            }
            price.Font = new Font("Microsoft YaHei", 7, FontStyle.Bold);
            price.Location = new Point(24, 15);
            price.Size = new Size(40, 14);
            card.Controls.Add(price);

            if (food.price > 0)
            {
                Label stockLabel = new Label();
                stockLabel.Text = "x" + stock;
                stockLabel.Font = new Font("Microsoft YaHei", 6);
                stockLabel.ForeColor = stock <= 0 ? Color.FromArgb(220, 100, 80) : Color.FromArgb(140, 130, 120);
                stockLabel.Location = new Point(62, 8);
                stockLabel.Size = new Size(22, 14);
                stockLabel.TextAlign = ContentAlignment.MiddleCenter;
                card.Controls.Add(stockLabel);
            }

            Button buyBtn = new Button();
            buyBtn.Text = "喂这个";
            buyBtn.FlatStyle = FlatStyle.Flat;
            buyBtn.FlatAppearance.BorderSize = 0;
            buyBtn.Font = new Font("Microsoft YaHei", 7, FontStyle.Bold);
            buyBtn.Location = new Point(82, 2);
            buyBtn.Size = new Size(46, 28);
            buyBtn.Cursor = Cursors.Hand;

            bool cdReady = (DateTime.Now - pet.GetLastFeedTime()).TotalHours >= 4;
            bool canFeed = pet.Intimacy < 999 && cdReady && (stock > 0 || food.price == 0);

            if (canFeed)
            {
                buyBtn.BackColor = food.price == 0 ? Color.FromArgb(100, 190, 120) : pet.GetCurrentSkinSecondaryColor();
                buyBtn.ForeColor = Color.White;
            }
            else
            {
                buyBtn.BackColor = Color.FromArgb(210, 205, 200);
                buyBtn.ForeColor = Color.FromArgb(150, 145, 140);
                buyBtn.Enabled = false;
            }

            PetForm.FoodDef capturedFood = food;
            buyBtn.Click += (s, e) =>
            {
                if (pet.BuyFood(capturedFood))
                {
                    HideFoodPanel();
                    RefreshState();
                }
            };
            card.Controls.Add(buyBtn);

            return card;
        }
        public void ResetFoodPanel() { HideFoodPanel(); }
        public void RefreshState()
        {
            bool ch = pet.CanHandshake(), cf = pet.CanFeed(); handBtn.Enabled = ch; feedBtn.Enabled = cf;
            handBtn.BackColor = ch ? pet.GetCurrentSkinPrimaryColor() : Color.FromArgb(210, 200, 195);
            feedBtn.BackColor = cf ? pet.GetCurrentSkinSecondaryColor() : Color.FromArgb(210, 200, 195);
            handCdLabel.Text = ch ? "" : pet.GetHandCdRemaining(); feedCdLabel.Text = cf ? "" : pet.GetFeedCdRemaining();
        }
    }

    // ==================== 任务系统界面 ====================
    public class QuestForm : Form
    {
        private PetForm pet;
        private Label crystalLabel, pageTitle;
        private FlowLayoutPanel contentPanel;
        private Button btnPrev, btnNext;
        private int currentPage = 0; // 0=每日任务, 1=里程碑成就

        public QuestForm(PetForm owner)
        {
            this.pet = owner;
            this.Text = "任务系统";
            this.Size = new Size(380, 435);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(255, 252, 245);
            this.Font = new Font("Microsoft YaHei", 9);
            try { this.Icon = new Icon(Path.Combine(Application.StartupPath, "app_icon.ico")); } catch { }

            // 顶部水晶栏
            Panel topBar = new Panel();
            topBar.BackColor = pet.GetCurrentSkinPrimaryColor();
            topBar.Location = new Point(0, 0);
            topBar.Size = new Size(380, 4);
            this.Controls.Add(topBar);

            Panel crystalPanel = new Panel();
            crystalPanel.BackColor = Color.FromArgb(255, 248, 235);
            crystalPanel.Location = new Point(0, 4);
            crystalPanel.Size = new Size(380, 44);
            this.Controls.Add(crystalPanel);

            crystalLabel = new Label();
            crystalLabel.Text = "\uD83D\uDC8E " + pet.GetCrystals();
            crystalLabel.Font = new Font("Microsoft YaHei", 14, FontStyle.Bold);
            crystalLabel.ForeColor = Color.FromArgb(80, 175, 210);
            crystalLabel.Location = new Point(20, 6);
            crystalLabel.Size = new Size(180, 32);
            crystalLabel.TextAlign = ContentAlignment.MiddleLeft;
            crystalPanel.Controls.Add(crystalLabel);

            // 翻页导航栏
            Panel navBar = new Panel();
            navBar.Location = new Point(0, 50);
            navBar.Size = new Size(380, 28);
            navBar.BackColor = Color.Transparent;
            this.Controls.Add(navBar);

            btnPrev = new Button();
            btnPrev.Text = "<";
            btnPrev.FlatStyle = FlatStyle.Flat;
            btnPrev.FlatAppearance.BorderSize = 0;
            btnPrev.Font = new Font("Microsoft YaHei", 10, FontStyle.Bold);
            btnPrev.ForeColor = pet.GetCurrentSkinPrimaryColor();
            btnPrev.BackColor = Color.Transparent;
            btnPrev.Location = new Point(110, 0);
            btnPrev.Size = new Size(30, 28);
            btnPrev.Cursor = Cursors.Hand;
            btnPrev.Click += (s, e) => { if (currentPage > 0) { currentPage--; RefreshUI(); } };
            navBar.Controls.Add(btnPrev);

            pageTitle = new Label();
            pageTitle.Text = "每日任务";
            pageTitle.Font = new Font("Microsoft YaHei", 10, FontStyle.Bold);
            pageTitle.ForeColor = Color.FromArgb(100, 85, 65);
            pageTitle.Location = new Point(140, 0);
            pageTitle.Size = new Size(100, 28);
            pageTitle.TextAlign = ContentAlignment.MiddleCenter;
            navBar.Controls.Add(pageTitle);

            btnNext = new Button();
            btnNext.Text = ">";
            btnNext.FlatStyle = FlatStyle.Flat;
            btnNext.FlatAppearance.BorderSize = 0;
            btnNext.Font = new Font("Microsoft YaHei", 10, FontStyle.Bold);
            btnNext.ForeColor = pet.GetCurrentSkinPrimaryColor();
            btnNext.BackColor = Color.Transparent;
            btnNext.Location = new Point(240, 0);
            btnNext.Size = new Size(30, 28);
            btnNext.Cursor = Cursors.Hand;
            btnNext.Click += (s, e) => { if (currentPage < 1) { currentPage++; RefreshUI(); } };
            navBar.Controls.Add(btnNext);

            // 内容面板
            contentPanel = new FlowLayoutPanel();
            contentPanel.Location = new Point(10, 80);
            contentPanel.Size = new Size(355, 260);
            contentPanel.AutoScroll = true;
            contentPanel.BackColor = Color.Transparent;
            this.Controls.Add(contentPanel);

            // 关闭按钮
            Button closeBtn = new Button();
            closeBtn.Text = "关闭";
            closeBtn.FlatStyle = FlatStyle.Flat;
            closeBtn.BackColor = pet.GetCurrentSkinPrimaryColor();
            closeBtn.ForeColor = Color.White;
            closeBtn.Font = new Font("Microsoft YaHei", 10, FontStyle.Bold);
            closeBtn.Location = new Point(140, 348);
            closeBtn.Size = new Size(100, 32);
            closeBtn.FlatAppearance.BorderSize = 0;
            closeBtn.Cursor = Cursors.Hand;
            closeBtn.Click += (s, e) => { this.Close(); };
            this.Controls.Add(closeBtn);

            RefreshUI();
        }

        public void RefreshUI()
        {
            crystalLabel.Text = "\uD83D\uDC8E " + pet.GetCrystals();
            btnPrev.Visible = (currentPage > 0);
            btnNext.Visible = (currentPage < 1);
            contentPanel.Controls.Clear();

            if (currentPage == 0)
            {
                pageTitle.Text = "每日任务";
                foreach (var q in pet.GetAllQuestDefs())
                {
                    Panel card = CreateQuestCard(q);
                    contentPanel.Controls.Add(card);
                }
            }
            else
            {
                pageTitle.Text = "里程碑成就";
                foreach (var q in pet.GetAllMilestoneQuestDefs())
                {
                    Panel card = CreateMilestoneCard(q);
                    contentPanel.Controls.Add(card);
                }
            }
        }

        private Panel CreateQuestCard(PetForm.QuestDef q)
        {
            int prog = pet.GetQuestProgress(q.id);
            bool claimed = pet.IsQuestClaimed(q.id);
            bool canClaim = pet.CanClaimQuest(q.id);
            int pct = q.target > 0 ? Math.Min(100, prog * 100 / q.target) : 100;

            Panel card = new Panel();
            card.Size = new Size(335, 45);
            card.BackColor = claimed ? Color.FromArgb(240, 240, 235) : Color.White;
            card.BorderStyle = BorderStyle.FixedSingle;
            card.Margin = new Padding(0, 0, 0, 3);

            Label icon = new Label();
            icon.Text = q.icon;
            icon.Font = new Font("Segoe UI Emoji", 12);
            icon.Location = new Point(4, 4);
            icon.Size = new Size(24, 24);
            icon.TextAlign = ContentAlignment.MiddleCenter;
            card.Controls.Add(icon);

            Label name = new Label();
            name.Text = q.name;
            name.Font = new Font("Microsoft YaHei", 8, FontStyle.Bold);
            name.ForeColor = claimed ? Color.FromArgb(150, 150, 140) : Color.FromArgb(60, 50, 40);
            name.Location = new Point(30, 2);
            name.Size = new Size(120, 16);
            card.Controls.Add(name);

            Panel barBg = new Panel();
            barBg.BackColor = Color.FromArgb(235, 230, 225);
            barBg.Location = new Point(30, 20);
            barBg.Size = new Size(150, 8);
            card.Controls.Add(barBg);

            Panel barFill = new Panel();
            barFill.BackColor = claimed ? Color.FromArgb(170, 200, 170) : (canClaim ? Color.FromArgb(255, 190, 60) : pet.GetCurrentSkinPrimaryColor());
            barFill.Location = new Point(0, 0);
            barFill.Size = new Size(150 * pct / 100, 8);
            barBg.Controls.Add(barFill);

            Label progLabel = new Label();
            progLabel.Text = prog + "/" + q.target;
            progLabel.Font = new Font("Microsoft YaHei", 6);
            progLabel.ForeColor = Color.FromArgb(150, 140, 130);
            progLabel.Location = new Point(30, 28);
            progLabel.Size = new Size(70, 14);
            card.Controls.Add(progLabel);

            Label rewardLabel = new Label();
            rewardLabel.Text = "\uD83D\uDC8E" + q.reward;
            rewardLabel.Font = new Font("Microsoft YaHei", 7);
            rewardLabel.ForeColor = Color.FromArgb(80, 175, 210);
            rewardLabel.Location = new Point(100, 28);
            rewardLabel.Size = new Size(50, 14);
            rewardLabel.TextAlign = ContentAlignment.MiddleLeft;
            card.Controls.Add(rewardLabel);

            Button claimBtn = new Button();
            bool isDailyLogin = (q.id == "daily_login");
            if (isDailyLogin)
            {
                claimBtn.Text = claimed ? "已签到" : "签到";
                claimBtn.Enabled = !claimed;
                claimBtn.Cursor = claimed ? Cursors.Default : Cursors.Hand;
                if (claimed) { claimBtn.BackColor = Color.FromArgb(200, 200, 195); claimBtn.ForeColor = Color.White; }
                else { claimBtn.BackColor = pet.GetCurrentSkinPrimaryColor(); claimBtn.ForeColor = Color.White; }
            }
            else
            {
                claimBtn.Text = claimed ? "已领取" : (canClaim ? "领取" : "进行中");
                claimBtn.Enabled = canClaim;
                claimBtn.Cursor = canClaim ? Cursors.Hand : Cursors.Default;
                if (canClaim) { claimBtn.BackColor = Color.FromArgb(255, 190, 60); claimBtn.ForeColor = Color.White; }
                else if (claimed) { claimBtn.BackColor = Color.FromArgb(200, 200, 195); claimBtn.ForeColor = Color.White; }
                else { claimBtn.BackColor = Color.FromArgb(220, 215, 210); claimBtn.ForeColor = Color.FromArgb(150, 145, 140); }
            }
            claimBtn.FlatStyle = FlatStyle.Flat;
            claimBtn.FlatAppearance.BorderSize = 0;
            claimBtn.Font = new Font("Microsoft YaHei", 7, FontStyle.Bold);
            claimBtn.Location = new Point(248, 8);
            claimBtn.Size = new Size(78, 25);
            string qid = q.id;
            if (isDailyLogin)
                claimBtn.Click += (s, e) => { pet.DoDailyCheckin(); RefreshUI(); };
            else
                claimBtn.Click += (s, e) => { pet.ClaimQuest(qid); RefreshUI(); };
            card.Controls.Add(claimBtn);

            return card;
        }

        private Panel CreateMilestoneCard(PetForm.QuestDef q)
        {
            int prog = pet.GetMilestoneQuestProgress(q.id);
            bool claimed = pet.IsMilestoneQuestClaimed(q.id);
            bool canClaim = pet.CanClaimMilestoneQuest(q.id);
            int pct = q.target > 0 ? Math.Min(100, prog * 100 / q.target) : 100;

            Panel card = new Panel();
            card.Size = new Size(335, 45);
            card.BackColor = claimed ? Color.FromArgb(240, 240, 235) : Color.FromArgb(252, 250, 242);
            card.BorderStyle = BorderStyle.FixedSingle;
            card.Margin = new Padding(0, 0, 0, 3);

            Label icon = new Label();
            icon.Text = q.icon;
            icon.Font = new Font("Segoe UI Emoji", 12);
            icon.Location = new Point(4, 4);
            icon.Size = new Size(24, 24);
            icon.TextAlign = ContentAlignment.MiddleCenter;
            card.Controls.Add(icon);

            Label name = new Label();
            name.Text = q.name;
            name.Font = new Font("Microsoft YaHei", 8, FontStyle.Bold);
            name.ForeColor = claimed ? Color.FromArgb(150, 150, 140) : Color.FromArgb(80, 60, 30);
            name.Location = new Point(30, 2);
            name.Size = new Size(120, 16);
            card.Controls.Add(name);

            Panel barBg = new Panel();
            barBg.BackColor = Color.FromArgb(235, 230, 225);
            barBg.Location = new Point(30, 20);
            barBg.Size = new Size(150, 8);
            card.Controls.Add(barBg);

            Panel barFill = new Panel();
            barFill.BackColor = claimed ? Color.FromArgb(170, 200, 170) : (canClaim ? Color.FromArgb(255, 190, 60) : Color.FromArgb(180, 160, 140));
            barFill.Location = new Point(0, 0);
            barFill.Size = new Size(150 * pct / 100, 8);
            barBg.Controls.Add(barFill);

            Label progLabel = new Label();
            progLabel.Text = prog + "/" + q.target;
            progLabel.Font = new Font("Microsoft YaHei", 6);
            progLabel.ForeColor = Color.FromArgb(150, 140, 130);
            progLabel.Location = new Point(30, 28);
            progLabel.Size = new Size(70, 14);
            card.Controls.Add(progLabel);

            Label rewardLabel = new Label();
            rewardLabel.Text = "\uD83D\uDC8E" + q.reward;
            rewardLabel.Font = new Font("Microsoft YaHei", 7);
            rewardLabel.ForeColor = Color.FromArgb(80, 175, 210);
            rewardLabel.Location = new Point(100, 28);
            rewardLabel.Size = new Size(50, 14);
            rewardLabel.TextAlign = ContentAlignment.MiddleLeft;
            card.Controls.Add(rewardLabel);

            Button claimBtn = new Button();
            claimBtn.Text = claimed ? "已领取" : (canClaim ? "领取" : "进行中");
            claimBtn.FlatStyle = FlatStyle.Flat;
            claimBtn.FlatAppearance.BorderSize = 0;
            claimBtn.Font = new Font("Microsoft YaHei", 7, FontStyle.Bold);
            claimBtn.Location = new Point(248, 8);
            claimBtn.Size = new Size(78, 25);
            claimBtn.Cursor = canClaim ? Cursors.Hand : Cursors.Default;
            claimBtn.Enabled = canClaim;
            if (canClaim) { claimBtn.BackColor = Color.FromArgb(255, 190, 60); claimBtn.ForeColor = Color.White; }
            else if (claimed) { claimBtn.BackColor = Color.FromArgb(200, 200, 195); claimBtn.ForeColor = Color.White; }
            else { claimBtn.BackColor = Color.FromArgb(220, 215, 210); claimBtn.ForeColor = Color.FromArgb(150, 145, 140); }
            string qid = q.id;
            claimBtn.Click += (s, e) => { pet.ClaimMilestoneQuest(qid); RefreshUI(); };
            card.Controls.Add(claimBtn);

            return card;
        }
    }

    // ==================== 宠物面板泡泡入口 ====================
    public class PanelBubble : Form
    {
        private PetForm pet;

        public PanelBubble(PetForm owner)
        {
            this.pet = owner;
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.Manual;
            this.Size = new Size(52, 52);
            this.BackColor = Color.FromArgb(100, 170, 235);

            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddEllipse(0, 0, 52, 52);
            this.Region = new Region(path);

            this.Paint += OnPaint;
            this.Click += (s, e) => { pet.OpenDetails(this, EventArgs.Empty); this.Hide(); };
            this.MouseDown += (s, e) => { pet.OpenDetails(this, EventArgs.Empty); this.Hide(); };
            this.Cursor = Cursors.Hand;
        }

        private void OnPaint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // 主体蓝色渐变圆
            using (System.Drawing.Drawing2D.GraphicsPath body = new System.Drawing.Drawing2D.GraphicsPath())
            {
                body.AddEllipse(0, 0, 52, 52);
                using (System.Drawing.Drawing2D.PathGradientBrush brush = new System.Drawing.Drawing2D.PathGradientBrush(body))
                {
                    brush.CenterPoint = new PointF(18, 16);
                    brush.CenterColor = Color.FromArgb(250, 200, 225, 255);
                    brush.SurroundColors = new Color[] { Color.FromArgb(240, 100, 170, 235) };
                    e.Graphics.FillEllipse(brush, 0, 0, 52, 52);
                }
            }
            // 高光
            using (System.Drawing.Drawing2D.GraphicsPath highlight = new System.Drawing.Drawing2D.GraphicsPath())
            {
                highlight.AddEllipse(11, 8, 20, 16);
                using (System.Drawing.Drawing2D.PathGradientBrush brush = new System.Drawing.Drawing2D.PathGradientBrush(highlight))
                {
                    brush.CenterColor = Color.FromArgb(160, 255, 255, 255);
                    brush.SurroundColors = new Color[] { Color.FromArgb(0, 255, 255, 255) };
                    e.Graphics.FillEllipse(brush, 11, 8, 20, 16);
                }
            }
            // 文字
            using (Font f = new Font("Microsoft YaHei", 8, FontStyle.Bold))
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;
                e.Graphics.DrawString("宠物面板", f, new SolidBrush(Color.FromArgb(230, 35, 65, 115)), new RectangleF(0, 1, 52, 52), sf);
            }
        }
    }

    // ==================== 商城界面 ====================
    public class ShopForm : Form
    {
        private PetForm pet;
        private Label crystalLabel;
        private FlowLayoutPanel itemPanel;

        public ShopForm(PetForm owner)
        {
            this.pet = owner;
            this.Text = "商城";
            this.Size = new Size(380, 530);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(255, 252, 245);
            this.Font = new Font("Microsoft YaHei", 9);
            try { this.Icon = new Icon(Path.Combine(Application.StartupPath, "app_icon.ico")); } catch { }

            Panel topBar = new Panel();
            topBar.BackColor = pet.GetCurrentSkinPrimaryColor();
            topBar.Location = new Point(0, 0);
            topBar.Size = new Size(380, 4);
            this.Controls.Add(topBar);

            Label title = new Label();
            title.Text = "🛒 宠物商城";
            title.Font = new Font("Microsoft YaHei", 12, FontStyle.Bold);
            title.ForeColor = Color.FromArgb(80, 50, 40);
            title.Location = new Point(20, 10);
            title.Size = new Size(200, 28);
            title.TextAlign = ContentAlignment.MiddleLeft;
            this.Controls.Add(title);

            crystalLabel = new Label();
            crystalLabel.Font = new Font("Microsoft YaHei", 10, FontStyle.Bold);
            crystalLabel.ForeColor = Color.FromArgb(80, 175, 210);
            crystalLabel.Location = new Point(240, 10);
            crystalLabel.Size = new Size(120, 28);
            crystalLabel.TextAlign = ContentAlignment.MiddleRight;
            this.Controls.Add(crystalLabel);

            itemPanel = new FlowLayoutPanel();
            itemPanel.Location = new Point(15, 48);
            itemPanel.Size = new Size(350, 400);
            itemPanel.FlowDirection = FlowDirection.LeftToRight;
            itemPanel.WrapContents = true;
            itemPanel.AutoScroll = true;
            itemPanel.BackColor = Color.Transparent;
            this.Controls.Add(itemPanel);

            Button closeBtn = new Button();
            closeBtn.Text = "关闭";
            closeBtn.Location = new Point(140, 478);
            closeBtn.Size = new Size(100, 30);
            closeBtn.BackColor = pet.GetCurrentSkinPrimaryColor();
            closeBtn.ForeColor = Color.White;
            closeBtn.FlatStyle = FlatStyle.Flat;
            closeBtn.FlatAppearance.BorderSize = 0;
            closeBtn.Font = new Font("Microsoft YaHei", 9);
            closeBtn.Click += (s, e) => { this.Hide(); };
            this.Controls.Add(closeBtn);

            this.FormClosing += (s, e) => { this.Hide(); e.Cancel = true; };
            RefreshUI();
        }

        public void RefreshUI()
        {
            crystalLabel.Text = "💎 " + pet.GetCrystals();
            itemPanel.Controls.Clear();
            for (int i = 1; i < pet.foodDefs.Length; i++)
            {
                var food = pet.foodDefs[i];
                int stock = pet.GetFoodStock(i);
                Panel card = CreateShopCard(food, i, stock);
                itemPanel.Controls.Add(card);
            }
            // 商城专属物品
            for (int i = 0; i < pet.shopOnlyDefs.Length; i++)
            {
                Panel card = CreateShopOnlyCard(pet.shopOnlyDefs[i], i);
                itemPanel.Controls.Add(card);
            }
        }

        private Panel CreateShopCard(PetForm.FoodDef food, int index, int stock)
        {
            Panel card = new Panel();
            card.Size = new Size(330, 60);
            card.BackColor = Color.White;
            card.BorderStyle = BorderStyle.FixedSingle;

            Label icon = new Label();
            icon.Text = food.icon;
            icon.Font = new Font("Segoe UI Emoji", 18);
            icon.Location = new Point(8, 8);
            icon.Size = new Size(40, 40);
            icon.TextAlign = ContentAlignment.MiddleCenter;
            card.Controls.Add(icon);

            Label name = new Label();
            name.Text = food.name;
            name.Font = new Font("Microsoft YaHei", 10, FontStyle.Bold);
            name.ForeColor = Color.FromArgb(60, 50, 40);
            name.Location = new Point(54, 6);
            name.Size = new Size(80, 20);
            card.Controls.Add(name);

            Label intimacy = new Label();
            intimacy.Text = "+" + food.intimacy + " 亲密度";
            intimacy.Font = new Font("Microsoft YaHei", 8);
            intimacy.ForeColor = Color.FromArgb(100, 180, 120);
            intimacy.Location = new Point(54, 26);
            intimacy.Size = new Size(80, 16);
            card.Controls.Add(intimacy);

            Label price = new Label();
            price.Text = "💎" + food.price;
            price.ForeColor = Color.FromArgb(80, 175, 210);
            price.Font = new Font("Microsoft YaHei", 9, FontStyle.Bold);
            price.Location = new Point(140, 8);
            price.Size = new Size(60, 24);
            price.TextAlign = ContentAlignment.MiddleCenter;
            card.Controls.Add(price);

            Label stockLabel = new Label();
            stockLabel.Text = "拥有 " + stock;
            stockLabel.Font = new Font("Microsoft YaHei", 7);
            stockLabel.ForeColor = stock <= 0 ? Color.FromArgb(180, 160, 140) : Color.FromArgb(100, 140, 100);
            stockLabel.Location = new Point(140, 34);
            stockLabel.Size = new Size(55, 14);
            stockLabel.TextAlign = ContentAlignment.MiddleCenter;
            card.Controls.Add(stockLabel);

            Button buyBtn = new Button();
            buyBtn.Text = "购买";
            buyBtn.FlatStyle = FlatStyle.Flat;
            buyBtn.FlatAppearance.BorderSize = 0;
            buyBtn.Font = new Font("Microsoft YaHei", 8, FontStyle.Bold);
            buyBtn.Location = new Point(200, 8);
            buyBtn.Size = new Size(120, 40);
            buyBtn.Cursor = Cursors.Hand;

            bool canAfford = pet.GetCrystals() >= food.price;
            bool hasRoom = stock < pet.foodMaxStock[index];

            if (canAfford && hasRoom)
            {
                buyBtn.BackColor = pet.GetCurrentSkinSecondaryColor();
                buyBtn.ForeColor = Color.White;
            }
            else
            {
                buyBtn.BackColor = Color.FromArgb(210, 205, 200);
                buyBtn.ForeColor = Color.FromArgb(150, 145, 140);
                buyBtn.Enabled = false;
            }

            int capturedIndex = index;
            buyBtn.Click += (s, e) =>
            {
                if (pet.BuyFromShop(capturedIndex))
                {
                    RefreshUI();
                }
            };
            card.Controls.Add(buyBtn);

            return card;
        }

        private Panel CreateShopOnlyCard(PetForm.ShopOnlyDef item, int index)
        {
            Panel card = new Panel();
            card.Size = new Size(330, 60);
            card.BackColor = Color.FromArgb(255, 245, 235);
            card.BorderStyle = BorderStyle.FixedSingle;

            Label icon = new Label();
            icon.Text = item.icon;
            icon.Font = new Font("Segoe UI Emoji", 18);
            icon.Location = new Point(8, 8);
            icon.Size = new Size(40, 40);
            icon.TextAlign = ContentAlignment.MiddleCenter;
            card.Controls.Add(icon);

            Label name = new Label();
            name.Text = item.name;
            name.Font = new Font("Microsoft YaHei", 10, FontStyle.Bold);
            name.ForeColor = Color.FromArgb(200, 90, 60);
            name.Location = new Point(54, 4);
            name.Size = new Size(80, 20);
            card.Controls.Add(name);

            Label price = new Label();
            price.Text = "\uD83D\uDC8E" + item.price + " \u66F6";
            price.ForeColor = Color.FromArgb(220, 130, 80);
            price.Font = new Font("Microsoft YaHei", 9, FontStyle.Bold);
            price.Location = new Point(140, 6);
            price.Size = new Size(60, 20);
            price.TextAlign = ContentAlignment.MiddleCenter;
            card.Controls.Add(price);

            Label limit = new Label();
            limit.Text = "\u9650\u91CF 1 \u6B21";
            limit.Font = new Font("Microsoft YaHei", 8);
            limit.ForeColor = pet.shopOnlyPurchased[index] ? Color.FromArgb(180, 160, 140) : Color.FromArgb(220, 130, 80);
            limit.Location = new Point(54, 24);
            limit.Size = new Size(100, 14);
            card.Controls.Add(limit);

            Label statusLabel = new Label();
            statusLabel.Font = new Font("Microsoft YaHei", 7);
            statusLabel.Location = new Point(140, 34);
            statusLabel.Size = new Size(55, 14);
            statusLabel.TextAlign = ContentAlignment.MiddleCenter;
            card.Controls.Add(statusLabel);

            Button buyBtn = new Button();
            buyBtn.FlatStyle = FlatStyle.Flat;
            buyBtn.FlatAppearance.BorderSize = 0;
            buyBtn.Font = new Font("Microsoft YaHei", 8, FontStyle.Bold);
            buyBtn.Location = new Point(200, 8);
            buyBtn.Size = new Size(120, 40);
            buyBtn.Cursor = Cursors.Hand;

            bool purchased = pet.shopOnlyPurchased[index];
            bool canAfford = pet.GetCrystals() >= item.price;

            if (purchased)
            {
                buyBtn.Text = "已兑换";
                buyBtn.BackColor = Color.FromArgb(210, 205, 200);
                buyBtn.ForeColor = Color.FromArgb(150, 145, 140);
                buyBtn.Enabled = false;
                statusLabel.Text = "已兑换";
                statusLabel.ForeColor = Color.FromArgb(100, 180, 100);
            }
            else if (canAfford)
            {
                buyBtn.Text = "兑换";
                buyBtn.BackColor = Color.FromArgb(240, 140, 80);
                buyBtn.ForeColor = Color.White;
                statusLabel.Text = "可兑换";
                statusLabel.ForeColor = Color.FromArgb(220, 130, 80);
            }
            else
            {
                buyBtn.Text = "兑换";
                buyBtn.BackColor = Color.FromArgb(210, 205, 200);
                buyBtn.ForeColor = Color.FromArgb(150, 145, 140);
                buyBtn.Enabled = false;
                statusLabel.Text = "💎不足";
                statusLabel.ForeColor = Color.FromArgb(180, 140, 120);
            }

            int capturedIndex = index;
            buyBtn.Click += (s, e) =>
            {
                if (pet.BuyShopOnly(capturedIndex))
                {
                    RefreshUI();
                }
            };
            card.Controls.Add(buyBtn);

            return card;
        }
    }

    // ==================== 统一游戏界面 ====================
    public class GameForm : Form
    {
        private PetForm pet;
        private enum State { SelectGame, SelectLevel, Playing }
        private State currentState = State.SelectGame;
        private Panel topBar, contentPanel;
        private Label titleLabel;
        private Button backBtn;
        private Image petImage;

        // 推箱子游戏状态
        private int currentLevel = 1;
        private int[,] grid;
        private int playerX, playerY;
        private int gridRows, gridCols;
        private const int CellSize = 56;
        private Panel gamePanel;
        private Label crystalLabel, statusLabel;
        private Button resetBtn;

        private static int[][,] levels = new int[10][,]
        {
            new int[,] { {1,1,1,1,1}, {1,0,0,0,1}, {1,0,3,2,1}, {1,0,4,0,1}, {1,1,1,1,1} },
            new int[,] { {1,1,1,1,1,1}, {1,0,0,0,0,1}, {1,0,3,0,2,1}, {1,0,0,4,0,1}, {1,1,1,1,1,1} },
            new int[,] { {1,1,1,1,1,1}, {1,0,0,2,0,1}, {1,0,3,0,0,1}, {1,0,3,4,0,1}, {1,0,0,2,0,1}, {1,1,1,1,1,1} },
            new int[,] { {1,1,1,1,1,1,1}, {1,0,0,2,0,0,1}, {1,0,3,0,0,3,1}, {1,0,0,4,0,2,1}, {1,1,1,1,1,1,1} },
            new int[,] { {1,1,1,1,1,1,1}, {1,2,0,0,0,2,1}, {1,0,1,3,1,0,1}, {1,0,3,4,3,0,1}, {1,0,0,2,0,0,1}, {1,1,1,1,1,1,1} },
            new int[,] { {1,1,1,1,1,1,1}, {1,2,0,0,0,2,1}, {1,0,0,1,0,0,1}, {1,0,3,0,3,0,1}, {1,0,0,4,0,0,1}, {1,0,3,2,0,0,1}, {1,1,1,1,1,1,1} },
            new int[,] { {1,1,1,1,1,1,1}, {1,0,0,0,0,0,1}, {1,0,3,0,3,0,1}, {1,2,0,4,0,2,1}, {1,0,3,0,0,2,1}, {1,0,0,0,0,0,1}, {1,1,1,1,1,1,1} },
            new int[,] { {1,1,1,1,1,1,1}, {1,2,0,0,0,2,1}, {1,0,3,0,3,0,1}, {1,0,0,4,0,0,1}, {1,0,3,0,3,0,1}, {1,2,0,0,0,2,1}, {1,1,1,1,1,1,1} },
            new int[,] { {1,1,1,1,1,1,1,1}, {1,0,0,0,0,0,0,1}, {1,0,2,0,0,0,2,1}, {1,0,0,3,0,3,0,1}, {1,0,1,0,4,1,0,1}, {1,0,0,3,0,3,0,1}, {1,0,2,0,0,0,2,1}, {1,0,0,0,0,0,0,1}, {1,1,1,1,1,1,1,1} },
            new int[,] { {1,1,1,1,1,1,1,1,1}, {1,2,0,0,0,0,0,2,1}, {1,0,3,0,0,0,3,0,1}, {1,0,0,0,0,0,0,0,1}, {1,0,0,0,4,0,0,0,1}, {1,0,0,0,3,0,0,0,1}, {1,0,3,0,0,0,3,0,1}, {1,2,0,0,2,0,0,2,1}, {1,1,1,1,1,1,1,1,1} }
        };
        private static int[] levelRewards = new int[] { 0, 10, 20, 30, 50, 80, 120, 150, 200, 250, 300 };

        public GameForm(PetForm owner)
        {
            this.pet = owner;
            this.Text = "小游戏";
            this.Size = new Size(420, 700);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(255, 252, 245);
            this.DoubleBuffered = true;
            this.KeyPreview = true;
            this.KeyDown += (s, e) => { }; // 保持KeyPreview激活
            this.Font = new Font("Microsoft YaHei", 9);

            try
            {
                string imgPath = System.IO.Path.Combine(Application.StartupPath, "微信图片_20260711143702_6_2.jpg");
                using (System.IO.FileStream fs = new System.IO.FileStream(imgPath, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                { petImage = Image.FromStream(fs); }
            }
            catch { }

            // 顶部栏
            topBar = new Panel();
            topBar.Location = new Point(0, 0);
            topBar.Size = new Size(410, 44);
            topBar.BackColor = Color.FromArgb(240, 235, 225);
            this.Controls.Add(topBar);

            backBtn = new Button();
            backBtn.Text = "\u2190 返回";
            backBtn.Font = new Font("Microsoft YaHei", 9);
            backBtn.FlatStyle = FlatStyle.Flat;
            backBtn.FlatAppearance.BorderSize = 0;
            backBtn.BackColor = Color.FromArgb(200, 190, 180);
            backBtn.ForeColor = Color.White;
            backBtn.Location = new Point(5, 7);
            backBtn.Size = new Size(70, 30);
            backBtn.Cursor = Cursors.Hand;
            backBtn.Click += (s, e) => GoBack();
            topBar.Controls.Add(backBtn);

            titleLabel = new Label();
            titleLabel.Font = new Font("Microsoft YaHei", 12, FontStyle.Bold);
            titleLabel.ForeColor = Color.FromArgb(80, 50, 40);
            titleLabel.Location = new Point(80, 7);
            titleLabel.Size = new Size(250, 30);
            titleLabel.TextAlign = ContentAlignment.MiddleCenter;
            topBar.Controls.Add(titleLabel);

            // 内容面板
            contentPanel = new Panel();
            contentPanel.Location = new Point(0, 44);
            contentPanel.Size = new Size(410, 615);
            contentPanel.BackColor = Color.FromArgb(255, 252, 245);
            contentPanel.AutoScroll = true;
            this.Controls.Add(contentPanel);

            ShowSelectGame();
        }

        private void ClearContent()
        {
            contentPanel.Controls.Clear();
        }

        private void GoBack()
        {
            if (currentState == State.SelectLevel)
                ShowSelectGame();
            else if (currentState == State.Playing)
                ShowSelectLevel();
        }

        // ==================== 选择游戏 ====================
        private void ShowSelectGame()
        {
            currentState = State.SelectGame;
            ClearContent();
            titleLabel.Text = "选择游戏";
            backBtn.Visible = false;

            Label subTitle = new Label();
            subTitle.Text = "选择一个游戏开始吧";
            subTitle.Font = new Font("Microsoft YaHei", 9);
            subTitle.ForeColor = Color.FromArgb(160, 140, 130);
            subTitle.Location = new Point(20, 10);
            subTitle.Size = new Size(370, 22);
            subTitle.TextAlign = ContentAlignment.MiddleCenter;
            contentPanel.Controls.Add(subTitle);

            Panel card = new Panel();
            card.Location = new Point(20, 45);
            card.Size = new Size(370, 140);
            card.BackColor = Color.White;
            card.BorderStyle = BorderStyle.FixedSingle;
            contentPanel.Controls.Add(card);

            Label icon = new Label();
            icon.Text = "\uD83D\uDCE6";
            icon.Font = new Font("Microsoft YaHei", 32);
            icon.Location = new Point(15, 15);
            icon.Size = new Size(55, 55);
            icon.TextAlign = ContentAlignment.MiddleCenter;
            card.Controls.Add(icon);

            Label gameName = new Label();
            gameName.Text = "推箱子";
            gameName.Font = new Font("Microsoft YaHei", 13, FontStyle.Bold);
            gameName.ForeColor = Color.FromArgb(80, 50, 40);
            gameName.Location = new Point(80, 15);
            gameName.Size = new Size(150, 28);
            card.Controls.Add(gameName);

            Label gameDesc = new Label();
            gameDesc.Text = "把箱子推到目标位置即可过关！";
            gameDesc.Font = new Font("Microsoft YaHei", 8);
            gameDesc.ForeColor = Color.FromArgb(150, 130, 120);
            gameDesc.Location = new Point(80, 45);
            gameDesc.Size = new Size(270, 20);
            card.Controls.Add(gameDesc);

            Label rewardInfo = new Label();
            rewardInfo.Text = "每关首次通过可获得水晶奖励";
            rewardInfo.Font = new Font("Microsoft YaHei", 8);
            rewardInfo.ForeColor = Color.FromArgb(180, 140, 100);
            rewardInfo.Location = new Point(80, 65);
            rewardInfo.Size = new Size(270, 20);
            card.Controls.Add(rewardInfo);

            Button playBtn = new Button();
            playBtn.Text = "选择关卡";
            playBtn.Font = new Font("Microsoft YaHei", 10, FontStyle.Bold);
            playBtn.FlatStyle = FlatStyle.Flat;
            playBtn.FlatAppearance.BorderSize = 0;
            playBtn.BackColor = Color.FromArgb(120, 180, 120);
            playBtn.ForeColor = Color.White;
            playBtn.Location = new Point(80, 95);
            playBtn.Size = new Size(150, 35);
            playBtn.Cursor = Cursors.Hand;
            playBtn.Click += (s, e) => ShowSelectLevel();
            card.Controls.Add(playBtn);
        }

        // ==================== 选择关卡 ====================
        private void ShowSelectLevel()
        {
            currentState = State.SelectLevel;
            ClearContent();
            titleLabel.Text = "选择关卡";
            backBtn.Visible = true;

            int[] rewards = new int[] { 0, 10, 20, 30, 50, 80, 120, 150, 200, 250, 300 };

            for (int i = 1; i <= 10; i++)
            {
                int level = i;
                bool unlocked = level <= pet.SokobanUnlockedLevel;
                bool rewarded = pet.SokobanRewarded[level];

                Panel card = new Panel();
                card.Location = new Point(15, 10 + (level - 1) * 62);
                card.Size = new Size(380, 55);
                card.BackColor = unlocked ? Color.White : Color.FromArgb(235, 230, 225);
                card.BorderStyle = BorderStyle.FixedSingle;
                contentPanel.Controls.Add(card);

                Label numLabel = new Label();
                numLabel.Text = unlocked ? level.ToString() : "\uD83D\uDD12";
                numLabel.Font = new Font("Microsoft YaHei", 14, FontStyle.Bold);
                numLabel.ForeColor = unlocked ? Color.FromArgb(80, 50, 40) : Color.FromArgb(180, 170, 160);
                numLabel.Location = new Point(10, 8);
                numLabel.Size = new Size(40, 35);
                numLabel.TextAlign = ContentAlignment.MiddleCenter;
                card.Controls.Add(numLabel);

                Label infoLabel = new Label();
                infoLabel.Text = "第" + level + "关";
                if (rewarded) infoLabel.Text += "  \u2714\uFE0F";
                infoLabel.Font = new Font("Microsoft YaHei", 11, FontStyle.Bold);
                infoLabel.ForeColor = unlocked ? Color.FromArgb(80, 50, 40) : Color.FromArgb(180, 170, 160);
                infoLabel.Location = new Point(55, 5);
                infoLabel.Size = new Size(120, 22);
                card.Controls.Add(infoLabel);

                Label rewardLabel = new Label();
                rewardLabel.Text = rewarded ? "已领取" : ("奖励 " + rewards[level] + " \uD83D\uDC8E");
                rewardLabel.Font = new Font("Microsoft YaHei", 8);
                rewardLabel.ForeColor = rewarded ? Color.FromArgb(100, 160, 100) : Color.FromArgb(180, 140, 100);
                rewardLabel.Location = new Point(55, 28);
                rewardLabel.Size = new Size(120, 18);
                card.Controls.Add(rewardLabel);

                Button enterBtn = new Button();
                enterBtn.Text = unlocked ? "进入" : "未解锁";
                enterBtn.Font = new Font("Microsoft YaHei", 9, FontStyle.Bold);
                enterBtn.FlatStyle = FlatStyle.Flat;
                enterBtn.FlatAppearance.BorderSize = 0;
                enterBtn.BackColor = unlocked ? Color.FromArgb(120, 180, 120) : Color.FromArgb(200, 195, 190);
                enterBtn.ForeColor = Color.White;
                enterBtn.Location = new Point(260, 10);
                enterBtn.Size = new Size(105, 32);
                enterBtn.Cursor = unlocked ? Cursors.Hand : Cursors.Default;
                enterBtn.Enabled = unlocked;
                int lv = level;
                enterBtn.Click += (s, e) => StartLevel(lv);
                card.Controls.Add(enterBtn);
            }
        }

        // ==================== 推箱子游戏 ====================
        private void StartLevel(int level)
        {
            currentState = State.Playing;
            currentLevel = level;
            ClearContent();
            titleLabel.Text = "推箱子 - 第" + level + "关";
            backBtn.Visible = true;

            // 游戏面板
            gamePanel = new Panel();
            gamePanel.Location = new Point(10, 5);
            gamePanel.Size = new Size(390, 390);
            gamePanel.BackColor = Color.FromArgb(245, 240, 230);
            gamePanel.BorderStyle = BorderStyle.FixedSingle;
            gamePanel.Paint += GamePanel_Paint;
            contentPanel.Controls.Add(gamePanel);

            // 状态标签
            statusLabel = new Label();
            statusLabel.Font = new Font("Microsoft YaHei", 10, FontStyle.Bold);
            statusLabel.ForeColor = Color.FromArgb(100, 160, 100);
            statusLabel.Location = new Point(10, 398);
            statusLabel.Size = new Size(390, 20);
            statusLabel.TextAlign = ContentAlignment.MiddleCenter;
            contentPanel.Controls.Add(statusLabel);

            // 底部按钮
            resetBtn = new Button();
            resetBtn.Text = "重置关卡";
            resetBtn.Font = new Font("Microsoft YaHei", 9, FontStyle.Bold);
            resetBtn.FlatStyle = FlatStyle.Flat;
            resetBtn.FlatAppearance.BorderSize = 0;
            resetBtn.BackColor = Color.FromArgb(220, 180, 140);
            resetBtn.ForeColor = Color.White;
            resetBtn.Location = new Point(140, 425);
            resetBtn.Size = new Size(120, 32);
            resetBtn.Cursor = Cursors.Hand;
            resetBtn.Click += (s, e) => { LoadLevel(currentLevel); };
            contentPanel.Controls.Add(resetBtn);

            bool rewarded = pet.SokobanRewarded[level];
            crystalLabel = new Label();
            crystalLabel.Font = new Font("Microsoft YaHei", 8);
            crystalLabel.ForeColor = rewarded ? Color.FromArgb(100, 160, 100) : Color.FromArgb(180, 140, 100);
            crystalLabel.Text = rewarded ? "奖励已领取" : ("奖励: " + levelRewards[level] + " \uD83D\uDC8E");
            crystalLabel.Location = new Point(10, 465);
            crystalLabel.Size = new Size(390, 20);
            crystalLabel.TextAlign = ContentAlignment.MiddleCenter;
            contentPanel.Controls.Add(crystalLabel);

            LoadLevel(level);
        }

        private void LoadLevel(int level)
        {
            int[,] template = levels[level - 1];
            gridRows = template.GetLength(0);
            gridCols = template.GetLength(1);
            grid = new int[gridRows, gridCols];
            Array.Copy(template, grid, template.Length);

            for (int r = 0; r < gridRows; r++)
                for (int c = 0; c < gridCols; c++)
                    if (grid[r, c] == 4) { playerX = c; playerY = r; }

            statusLabel.Text = "";
            if (gamePanel != null) gamePanel.Refresh();
        }

        private void GamePanel_Paint(object sender, PaintEventArgs e)
        {
            if (grid == null) return;
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int offsetX = (gamePanel.Width - gridCols * CellSize) / 2;
            int offsetY = (gamePanel.Height - gridRows * CellSize) / 2;

            for (int r = 0; r < gridRows; r++)
            {
                for (int c = 0; c < gridCols; c++)
                {
                    int x = offsetX + c * CellSize;
                    int y = offsetY + r * CellSize;
                    int cell = grid[r, c];

                    if (cell == 1)
                        g.FillRectangle(new SolidBrush(Color.FromArgb(140, 120, 100)), x, y, CellSize, CellSize);
                    else
                    {
                        g.FillRectangle(new SolidBrush(Color.FromArgb(245, 240, 230)), x, y, CellSize, CellSize);
                        if (cell == 2 || cell == 6)
                            g.FillEllipse(new SolidBrush(Color.FromArgb(255, 200, 180)), x + 12, y + 12, CellSize - 24, CellSize - 24);
                    }

                    if (cell == 3)
                    {
                        g.FillRectangle(new SolidBrush(Color.FromArgb(180, 140, 100)), x + 6, y + 6, CellSize - 12, CellSize - 12);
                        g.DrawRectangle(new Pen(Color.FromArgb(140, 100, 60), 2), x + 6, y + 6, CellSize - 12, CellSize - 12);
                        g.DrawLine(new Pen(Color.FromArgb(140, 100, 60), 1), x + CellSize / 2, y + 8, x + CellSize / 2, y + CellSize - 8);
                        g.DrawLine(new Pen(Color.FromArgb(140, 100, 60), 1), x + 8, y + CellSize / 2, x + CellSize - 8, y + CellSize / 2);
                    }
                    else if (cell == 5)
                    {
                        g.FillRectangle(new SolidBrush(Color.FromArgb(100, 180, 120)), x + 6, y + 6, CellSize - 12, CellSize - 12);
                        g.DrawRectangle(new Pen(Color.FromArgb(60, 140, 80), 2), x + 6, y + 6, CellSize - 12, CellSize - 12);
                    }

                    if (cell == 4 || cell == 6)
                    {
                        if (petImage != null)
                            g.DrawImage(petImage, x + 3, y + 3, CellSize - 6, CellSize - 6);
                        else
                            g.FillEllipse(new SolidBrush(Color.FromArgb(120, 180, 220)), x + 4, y + 4, CellSize - 8, CellSize - 8);
                    }
                }
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (currentState == State.Playing)
            {
                int dx = 0, dy = 0;
                if (keyData == Keys.Left || keyData == Keys.A) dx = -1;
                else if (keyData == Keys.Right || keyData == Keys.D) dx = 1;
                else if (keyData == Keys.Up || keyData == Keys.W) dy = -1;
                else if (keyData == Keys.Down || keyData == Keys.S) dy = 1;
                else return base.ProcessCmdKey(ref msg, keyData);
                MovePlayer(dx, dy);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void MovePlayer(int dx, int dy)
        {
            if (dx == 0 && dy == 0) return;
            int newX = playerX + dx;
            int newY = playerY + dy;
            if (grid[newY, newX] == 1) return;

            if (grid[newY, newX] == 3 || grid[newY, newX] == 5)
            {
                int boxNewX = newX + dx;
                int boxNewY = newY + dy;
                if (grid[boxNewY, boxNewX] == 1 || grid[boxNewY, boxNewX] == 3 || grid[boxNewY, boxNewX] == 5) return;

                int oldBoxCell = grid[newY, newX];
                grid[newY, newX] = (oldBoxCell == 5) ? 2 : 0;
                grid[boxNewY, boxNewX] = (grid[boxNewY, boxNewX] == 2) ? 5 : 3;
            }

            int oldPlayerCell = grid[playerY, playerX];
            grid[playerY, playerX] = (oldPlayerCell == 6) ? 2 : 0;
            grid[newY, newX] = (grid[newY, newX] == 2) ? 6 : 4;
            playerX = newX;
            playerY = newY;

            gamePanel.Invalidate();

            if (CheckWin())
            {
                OnLevelComplete();
            }
        }

        private bool CheckWin()
        {
            for (int r = 0; r < gridRows; r++)
                for (int c = 0; c < gridCols; c++)
                    if (grid[r, c] == 2 || grid[r, c] == 6) return false;
            return true;
        }

        private void OnLevelComplete()
        {
            statusLabel.Text = "恭喜过关！";
            statusLabel.ForeColor = Color.FromArgb(100, 180, 100);

            if (!pet.SokobanRewarded[currentLevel])
            {
                pet.SokobanRewarded[currentLevel] = true;
                pet.AddCrystals(levelRewards[currentLevel]);
                pet.SaveSokobanProgress();
                statusLabel.Text = "恭喜过关！获得 " + levelRewards[currentLevel] + " \uD83D\uDC8E";
                if (crystalLabel != null)
                    crystalLabel.Text = "已获得 " + levelRewards[currentLevel] + " \uD83D\uDC8E!";
            }

            if (currentLevel < 10 && pet.SokobanUnlockedLevel <= currentLevel)
            {
                pet.SokobanUnlockedLevel = currentLevel + 1;
                pet.SaveSokobanProgress();
            }

            Timer t = new Timer();
            t.Interval = 1500;
            t.Tick += (s, e) =>
            {
                t.Stop();
                if (currentLevel < 10 && pet.SokobanUnlockedLevel > currentLevel)
                    StartLevel(currentLevel + 1);
            };
            t.Start();
        }
    }
}
