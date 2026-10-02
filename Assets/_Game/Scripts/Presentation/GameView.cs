using System;
using System.Collections.Generic;
using Spotlight.Contracts;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Spotlight.Presentation
{
    public sealed class GameView : MonoBehaviour, IGameView
    {
        public string defaultLevelId = "level.prototype", towerId = "tower.basic";
        public uint seed = 1;
        public bool PreviewOnly;
        private GameViewContext services;
        private Canvas canvas;
        private Transform panel, actions, overlay;
        private Font font;
        private Text title, health, wave, skill, gold, note, selection;
        private readonly Text[] hands = new Text[6];
        private readonly Text[] items = new Text[2];
        private readonly List<IDisposable> subscriptions = new List<IDisposable>();
        private readonly HashSet<Guid> pending = new HashSet<Guid>();
        private readonly string[] ids = { "elm.water", "elm.fire", "elm.earth", "elm.wood", "elm.wind", "elm.thunder" };
        private readonly string[] names = { "水", "火", "土", "木", "风", "雷" };
        private readonly string[] itemIds = { "item.heal", "item.tower_stack" };
        private Guid itemPause, playerPause;
        private GameObject ownedEvents;
        private string placing, selectedItem;
        private CellCoord cell;
        private bool selected, move, swap, blocked;
        private float statusRefreshTimer;
        private GamePhase renderedPhase = (GamePhase)(-1);

        public void Bind(GameViewContext context)
        {
            if (context == null) throw new ArgumentNullException("context");
            Unbind(); services = context; BuildWidgets();
            subscriptions.Add(context.Events.Subscribe<CommandFinishedEvent>(delegate(CommandFinishedEvent e) { if (pending.Remove(e.Result.CommandId)) Result(e.Result); }));
            subscriptions.Add(context.Events.Subscribe<PhaseChangedEvent>(delegate(PhaseChangedEvent e) { CancelSelection(); RefreshAll(); }));
            subscriptions.Add(context.Events.Subscribe<BoardChangedEvent>(delegate(BoardChangedEvent e) { RefreshAll(); }));
            subscriptions.Add(context.Events.Subscribe<ResourcesChangedEvent>(delegate(ResourcesChangedEvent e) { RefreshAll(); }));
            subscriptions.Add(context.Events.Subscribe<HealthChangedEvent>(delegate(HealthChangedEvent e) { RefreshStatus(); }));
            subscriptions.Add(context.Events.Subscribe<WaveChangedEvent>(delegate(WaveChangedEvent e) { RefreshStatus(); }));
            subscriptions.Add(context.Events.Subscribe<SkillChangedEvent>(delegate(SkillChangedEvent e) { RefreshStatus(); }));
            subscriptions.Add(context.Events.Subscribe<DayEventChangedEvent>(delegate(DayEventChangedEvent e) { RefreshAll(); }));
            RefreshAll();
        }
        public void Unbind()
        {
            CancelSelection(); if (services != null && playerPause != Guid.Empty) services.Clock.ReleasePause(playerPause); playerPause = Guid.Empty;
            foreach (IDisposable s in subscriptions) s.Dispose(); subscriptions.Clear(); pending.Clear(); services = null;
            if (canvas != null) PresentationObjects.Release(canvas.gameObject); canvas = null; overlay = panel = actions = null;
            renderedPhase = (GamePhase)(-1);
        }
        private void Start() { if (PreviewOnly && services == null) TestWidgets(); }
        private void OnDestroy() { Unbind(); if (ownedEvents != null) PresentationObjects.Release(ownedEvents); ownedEvents = null; }
        private CommandContext Command() { return services.Commands.Create(services.Flow.GetSnapshot().Revision); }
        private void Result(OperationResult result)
        {
            if (result.State == OperationState.Queued) { pending.Add(result.CommandId); note.text = "正在处理…"; }
            else if (result.State == OperationState.Committed) { note.text = "完成"; RefreshAll(); }
            else note.text = ErrorText(result.Error);
        }
        private static string ErrorText(ErrorCode e)
        {
            switch (e) {
                case ErrorCode.InsufficientCost: case ErrorCode.InsufficientQuantity: return "数量不足";
                case ErrorCode.Occupied: return "这个格子已被占用";
                case ErrorCode.ReservedCell: case ErrorCode.InvalidPathPlacement: return "这里需要留作通道";
                case ErrorCode.WrongPhase: return "现在不能进行此操作";
                case ErrorCode.InvalidTarget: case ErrorCode.TargetDead: return "请选择有效目标";
                case ErrorCode.SkillAlreadyUsed: return "本夜技能已使用";
                case ErrorCode.SaveUnavailable: return "没有可继续的记录";
                case ErrorCode.InvalidDefinition: return "此内容尚未配置";
                case ErrorCode.EffectAlreadyActive: return "同类效果正在生效";
                case ErrorCode.VersionConflict: return "状态已变化，请重试";
                case ErrorCode.LimitReached: return "已达到数量上限";
                default: return "操作未完成，请重试";
            }
        }
        public void RefreshAll()
        {
            if (services == null || canvas == null) return; RefreshStatus();
            for (int i = 0; i < 6; i++) hands[i].text = names[i] + " × " + services.Resources.GetQuantity(ResourceBucket.Hand, ids[i]);
            items[0].text = "回复药剂 × " + services.Resources.GetQuantity(ResourceBucket.Inventory, itemIds[0]);
            items[1].text = "塔强化 × " + services.Resources.GetQuantity(ResourceBucket.Inventory, itemIds[1]);
            GamePhase phase = services.Flow.GetSnapshot().Phase;
            if (renderedPhase != phase) { renderedPhase = phase; DrawActions(phase); }
            DrawModal();
        }
        private void RefreshStatus()
        {
            if (services == null || title == null) return; FlowSnapshot f = services.Flow.GetSnapshot();
            title.text = "聚光灯 · 第 " + f.DayIndex + " 天 · " + PhaseName(f.Phase);
            ActorSnapshot spring; health.text = services.World.TryGetActor(services.World.SpringId, out spring) ? "灵泉 " + Mathf.CeilToInt(spring.CurrentHp) + " / " + Mathf.CeilToInt(spring.MaxHp) : "守护灵泉";
            WaveSnapshot w = services.Waves.GetSnapshot(); wave.text = "波次 " + w.WaveIndex + " · 敌人 " + w.AliveCount;
            SpecialSkillSnapshot s = services.Skill.GetSnapshot(); skill.text = "技能 " + s.UsesRemaining + " 次" + (s.RemainingSeconds > 0 ? " · 强化 " + Mathf.CeilToInt(s.RemainingSeconds) + " 秒" : "");
            gold.text = "金币 " + services.Resources.GetQuantity(ResourceBucket.Inventory, "res.gold");
        }
        private static string PhaseName(GamePhase p) { switch(p) { case GamePhase.Menu:return "开始旅程"; case GamePhase.Build:return "白天部署"; case GamePhase.Night:return "夜晚守护"; case GamePhase.DayEvent:return "今日事件"; case GamePhase.NightResult:return "守护成功"; case GamePhase.Ending:return "旅程完成"; case GamePhase.GameOver:return "灵泉失守"; default:return "迎接黎明"; } }
        private void DrawActions(GamePhase phase)
        {
            if (actions != null) PresentationObjects.Release(actions.gameObject); actions = Rect(panel, "Actions", 0, 0, 1020, 720);
            int index = 0;
            if (phase == GamePhase.Menu) { ActionButton("故事模式", index++, delegate { NewRun(GameMode.Story); }); ActionButton("无尽模式", index++, delegate { NewRun(GameMode.Endless); }); ActionButton("继续游戏", index++, Load); }
            if (phase == GamePhase.Build) {
                ActionButton("开始这一夜", index++, delegate { Result(services.Flow.TryStartNight(Command())); });
                ActionButton("保存进度", index++, delegate { Result(services.Saves.SaveCheckpoint(Command(), SaveReason.Manual)); });
                ActionButton("建造攻击塔", index++, delegate { Place(towerId); });
                ActionButton("移动所选", index++, delegate { ChooseMove(false); }); ActionButton("交换所选", index++, delegate { ChooseMove(true); });
                ActionButton("回收所选", index++, delegate { if (selected) Result(services.Deployment.TryRecall(Command(), cell)); else note.text = "先点选元素或塔"; });
                ActionButton("返回菜单", index++, Menu);
            }
            if (phase == GamePhase.Night) { ActionButton("释放特殊技", index++, delegate { Result(services.Skill.TryUse(Command())); }); ActionButton("返回菜单", index++, Menu); }
            if (phase == GamePhase.NightResult) {
                EndlessDefinition endless=services.Catalog.GetEndlessDefinition();bool milestone=services.Flow.GetSnapshot().Mode==GameMode.Endless&&endless!=null&&services.Flow.GetSnapshot().DayIndex==endless.MilestoneNight;
                if(milestone){ActionButton("继续无尽挑战",index++,delegate {Result(services.Flow.TryContinueEndless(Command()));});note.text="已完成无尽里程碑！继续挑战可领取本次里程碑奖励。";}
                else ActionButton("迎接下一天",index++,delegate {Result(services.Flow.TryConfirmNightResult(Command()));});
            }
            if (phase == GamePhase.GameOver) { ActionButton("从记录重试", index++, Load); ActionButton("返回菜单", index++, Menu); }
            if (phase == GamePhase.Ending) { ActionButton("返回菜单", index++, Menu); if (services.Flow.GetSnapshot().Mode == GameMode.Endless) ActionButton("继续无尽挑战", index++, delegate { Result(services.Flow.TryContinueEndless(Command())); }); }
        }
        private void ActionButton(string text, int i, Action action) { Button(actions, text, 24 + i % 4 * 236, 622 + i / 4 * 44, 220, 38, action); }
        private void NewRun(GameMode mode) { Result(services.Flow.TryNewRun(Command(), new NewRunRequest { LevelId = defaultLevelId, Mode = mode, Seed = seed })); }
        private void Menu() { Result(services.Flow.TryReturnToMenu(Command())); }
        private void Load() { SaveReadResult save = services.Saves.ReadCheckpoint(); if (save.Error != ErrorCode.None || save.Data == null) { note.text = ErrorText(save.Error); return; } Result(services.Flow.TryLoadRun(Command(), save.Data)); }
        private void Place(string id) { if (services == null || services.Flow.GetSnapshot().Phase != GamePhase.Build) return; placing = id; move = swap = false; note.text = "点击空格放置，或点取消"; }
        private void ChooseMove(bool exchange) { if (!selected) { note.text = "先点选元素或塔"; return; } placing = null; move = !exchange; swap = exchange; note.text = exchange ? "点击要交换的格子" : "点击要移往的空格"; }
        private void Update()
        {
            if(services!=null){statusRefreshTimer+=Time.unscaledDeltaTime;if(statusRefreshTimer>=0.2f){statusRefreshTimer=0;RefreshStatus();}}
            if (services == null || blocked || !Input.GetMouseButtonDown(0) || (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())) return;
            Camera camera = Camera.main; if (camera == null) return;
            Vector3 hit = camera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -camera.transform.position.z)); WorldPoint world = new WorldPoint(hit.x, hit.y);
            if (selectedItem != null) { SelectItemTarget(world); return; }
            if (services.Flow.GetSnapshot().Phase != GamePhase.Build) return;
            CellCoord clicked; if (!services.Board.TryWorldToCell(world, out clicked)) return;
            if (move || swap) { OperationResult r = swap ? services.Deployment.TrySwap(Command(), cell, clicked) : services.Deployment.TryMove(Command(), cell, clicked); move = swap = false; Result(r); return; }
            if (placing != null) { Result(services.Deployment.TryPlace(Command(), new PlacementRequest { Cell = clicked, DefinitionId = placing, Kind = placing == towerId ? OccupantKind.Tower : OccupantKind.ElementBlock })); return; }
            CellSnapshot c; selected = services.Board.TryGetCell(clicked, out c) && c.OccupantKind != OccupantKind.None; cell = clicked;
            selection.text = selected ? "已选格子（" + (cell.X + 1) + "，" + (cell.Y + 1) + "）" : "请选择元素或塔";
        }
        private void BeginItem(int index)
        {
            if (services == null) return; CancelSelection();
            if (services.Resources.GetQuantity(ResourceBucket.Inventory, itemIds[index]) <= 0) { note.text = "暂时没有这个道具"; return; }
            selectedItem = itemIds[index]; itemPause = services.Clock.AcquirePause(PauseReason.ItemSelection);
            if (index == 0) UseItem(services.World.SpringId); else note.text = "点击一座存活的塔强化，或点取消";
        }
        private void SelectItemTarget(WorldPoint point)
        {
            CellCoord coord; CellSnapshot c; if (!services.Board.TryWorldToCell(point, out coord) || !services.Board.TryGetCell(coord, out c) || c.OccupantKind != OccupantKind.Tower) { note.text = "请选择一座存活的塔"; return; }
            ActorSnapshot actor; if (!services.World.TryGetActor(c.OccupantId, out actor) || actor.CurrentHp <= 0) { note.text = "请选择一座存活的塔"; return; } UseItem(actor.Id);
        }
        private void UseItem(EntityId target) { OperationResult result = services.Items.TryUse(Command(), new ItemUseRequest { ItemId = selectedItem, Target = target }); ReleaseItem(); Result(result); }
        private void ReleaseItem() { selectedItem = null; if (services != null && itemPause != Guid.Empty) services.Clock.ReleasePause(itemPause); itemPause = Guid.Empty; }
        private void CancelSelection() { ReleaseItem(); placing = null; move = swap = false; }
        private void Pause() { if (services == null) return; if (playerPause == Guid.Empty) playerPause = services.Clock.AcquirePause(PauseReason.Player); else { services.Clock.ReleasePause(playerPause); playerPause = Guid.Empty; } note.text = playerPause == Guid.Empty ? "继续守护" : "已暂停"; }
        private void DrawModal()
        {
            if (overlay != null) PresentationObjects.Release(overlay.gameObject); overlay = null; blocked = false;
            FlowSnapshot flow = services.Flow.GetSnapshot();
            if (flow.Phase == GamePhase.DayEvent) {
                DayEventSnapshot day = services.DayEvents.GetSnapshot(); if (day == null || day.State == null || day.State.Resolved) return;
                Modal("今日事件", day.Text); int i = 0;
                if (day.Options != null) foreach (EventOptionDefinition option in day.Options) { string id = option.Id, instance = day.State.InstanceId; Button(overlay, option.Text, 260, 345 + i++ * 54, 760, 46, delegate { Result(services.DayEvents.TryChoose(Command(), instance, id)); }); }
            } else if (flow.Phase == GamePhase.Build && !flow.TutorialShown && !String.IsNullOrEmpty(services.Catalog.GetSettings().TutorialText)) {
                Modal("守护指南", services.Catalog.GetSettings().TutorialText); Button(overlay, "开始部署", 490, 530, 300, 48, delegate { Result(services.Flow.TryDismissTutorial(Command())); });
            }
        }
        private void Modal(string caption, string body)
        {
            overlay = Rect(panel, "Modal", 0, 0, 1280, 720); Image(overlay, new Color(0.02f,0.04f,0.08f,0.88f)); overlay.SetAsLastSibling(); blocked = true;
            Image(Rect(overlay, "Card", 220, 135, 840, 480), new Color(0.13f,0.19f,0.28f)); Label(overlay, caption, 260, 165, 760, 48, 30);
            Text text = Label(overlay, body, 260, 225, 760, 106, 21); text.alignment = TextAnchor.UpperLeft;
        }
        private void BuildWidgets()
        {
            font = CreateFont(); GameObject obj = new GameObject("Game UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); obj.transform.SetParent(transform,false);
            canvas = obj.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 50; CanvasScaler scaler = obj.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280,720); scaler.matchWidthOrHeight = 0.5f; panel = obj.transform;
            if (EventSystem.current == null) ownedEvents = new GameObject("UI Event System",typeof(EventSystem),typeof(StandaloneInputModule));
            Image(Rect(panel,"Header",0,0,1280,90),new Color(0.08f,0.12f,0.19f,0.97f)); Image(Rect(panel,"Sidebar",1020,90,260,630),new Color(0.08f,0.12f,0.19f,0.97f));
            title = Label(panel,"聚光灯",24,12,520,32,25); health = Label(panel,"灵泉",550,14,230,30,22); wave = Label(panel,"波次",800,14,220,30,20); note = Label(panel,"白天部署，夜晚守护灵泉。",24,53,980,26,17);
            gold = Label(panel,"金币 0",1040,104,220,30,21); skill = Label(panel,"技能 1 次",1040,140,220,30,18); selection = Label(panel,"请选择元素或塔",1040,180,220,46,16); Label(panel,"手牌 · 点击后放置",1040,238,220,24,18);
            for (int i=0;i<6;i++) { int index=i; hands[i]=Button(panel,names[i]+" × 0",1040+i%3*74,272+i/3*42,68,36,delegate { Place(ids[index]); }).GetComponentInChildren<Text>(); }
            Label(panel,"背包",1040,360,220,24,18); items[0]=Button(panel,"回复药剂 × 0",1040,392,220,36,delegate { BeginItem(0); }).GetComponentInChildren<Text>(); items[1]=Button(panel,"塔强化 × 0",1040,434,220,36,delegate { BeginItem(1); }).GetComponentInChildren<Text>();
            Button(panel,"取消选择",1040,484,220,34,delegate { CancelSelection(); note.text="选择已取消"; }); Button(panel,"暂停 / 继续",1040,528,220,36,Pause);
            Button(panel,"1 倍",1040,574,106,34,delegate { if (services!=null) Result(services.Clock.TrySetSpeed(Command(),GameSpeed.Normal)); }); Button(panel,"2 倍",1154,574,106,34,delegate { if (services!=null) Result(services.Clock.TrySetSpeed(Command(),GameSpeed.Double)); });
            Label(panel,"左键选格 · 右侧选择行动\n先部署，再开始夜晚",1040,632,220,58,16);
        }
        public void TestWidgets()
        {
            if (services!=null) return; if (canvas==null) BuildWidgets(); title.text="聚光灯 · 第 1 天 · 白天部署"; health.text="灵泉 100 / 100"; wave.text="波次 0 · 敌人 0"; gold.text="金币 20";
            for(int i=0;i<6;i++) hands[i].text=names[i]+" × 3"; items[0].text="回复药剂 × 2"; items[1].text="塔强化 × 2";
            if(actions!=null) PresentationObjects.Release(actions.gameObject); actions=Rect(panel,"Preview Actions",0,0,1020,720);
            ActionButton("开始这一夜（预览）",0,delegate { title.text="聚光灯 · 第 1 天 · 夜晚守护"; note.text="界面预览：游戏流程由正式场景运行。"; });
            ActionButton("查看事件（预览）",1,delegate { Modal("今日事件","旅人送来了守护灵泉的补给。\n选择一个选项继续。"); Button(overlay,"收下补给（预览）",420,420,440,50,delegate { PresentationObjects.Release(overlay.gameObject); overlay=null; blocked=false; note.text="预览：补给已收下"; }); });
            note.text="界面预览 · 按钮只展示交互，不改变游戏进度。";
        }
        public static Font CreateFont() { Font value=Font.CreateDynamicFontFromOSFont(new string[] {"Microsoft YaHei","SimHei","Arial"},20); return value!=null?value:Resources.GetBuiltinResource<Font>("Arial.ttf"); }
        private static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h) { GameObject o=new GameObject(name,typeof(RectTransform)); o.transform.SetParent(parent,false); RectTransform r=o.GetComponent<RectTransform>(); r.anchorMin=r.anchorMax=new Vector2(0,1); r.pivot=new Vector2(0,1); r.anchoredPosition=new Vector2(x,-y); r.sizeDelta=new Vector2(w,h); return r; }
        private static void Image(Transform t,Color color) { t.gameObject.AddComponent<Image>().color=color; }
        private Text Label(Transform parent,string text,float x,float y,float w,float h,int size) { Text t=Rect(parent,"Label",x,y,w,h).gameObject.AddComponent<Text>(); t.font=font;t.text=text;t.fontSize=size;t.color=new Color(0.94f,0.96f,1);t.alignment=TextAnchor.MiddleLeft;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;return t; }
        private Button Button(Transform parent,string text,float x,float y,float w,float h,Action action) { RectTransform r=Rect(parent,text,x,y,w,h);Image(r,new Color(0.21f,0.34f,0.46f));Button b=r.gameObject.AddComponent<Button>();Text l=Label(r,text,5,0,w-10,h,17);l.alignment=TextAnchor.MiddleCenter;b.onClick.AddListener(delegate {if(action!=null)action();});return b; }
    }
}

