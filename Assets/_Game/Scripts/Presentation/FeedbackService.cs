using System;
using System.Collections.Generic;
using Spotlight.Contracts;
using UnityEngine;
using UnityEngine.UI;

namespace Spotlight.Presentation
{
    public sealed class FeedbackService : MonoBehaviour, IFeedbackService
    {
        public AudioClip damageClip, reactionClip;
        private sealed class Floating { internal Text Text;internal Vector2 Origin;internal float Life; }
        private readonly List<IDisposable> subscriptions=new List<IDisposable>();
        private readonly Dictionary<long,WorldPoint> positions=new Dictionary<long,WorldPoint>();
        private readonly List<Floating> floating=new List<Floating>();
        private IWorldQuery world;private Canvas canvas;private Font font;private AudioSource sound;private float volume=1;
        public void Bind(IEventBus events,IWorldQuery query)
        {
            if(events==null||query==null)throw new ArgumentNullException();Unbind();world=query;
            foreach(ActorKind kind in new ActorKind[] {ActorKind.Spring,ActorKind.Tower,ActorKind.Enemy})foreach(ActorSnapshot a in query.GetActors(kind))positions[a.Id.Value]=a.Position;
            GameObject o=new GameObject("Combat Feedback",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));o.transform.SetParent(transform,false);canvas=o.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=75;
            CanvasScaler scale=o.GetComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1280,720);scale.matchWidthOrHeight=0.5f;font=GameView.CreateFont();
            sound=GetComponent<AudioSource>();if(sound==null)sound=gameObject.AddComponent<AudioSource>();sound.playOnAwake=false;sound.volume=volume;
            subscriptions.Add(events.Subscribe<ActorSpawnedEvent>(delegate(ActorSpawnedEvent e){Remember(e.Actor);}));
            subscriptions.Add(events.Subscribe<ActorMovedEvent>(delegate(ActorMovedEvent e){positions[e.Actor.Value]=e.Position;}));
            subscriptions.Add(events.Subscribe<DamageAppliedEvent>(delegate(DamageAppliedEvent e){if(e.Result==null||e.Result.Error!=ErrorCode.None||e.Result.FinalDamage<=0)return;WorldPoint p;if(Position(e.Result.Target,out p))Show("−"+Mathf.CeilToInt(e.Result.FinalDamage),p,new Color(1,0.65f,0.35f));Play(damageClip);}));
            subscriptions.Add(events.Subscribe<ReactionResolvedEvent>(delegate(ReactionResolvedEvent e){Show("元素反应",e.Position,new Color(0.5f,0.9f,1));Play(reactionClip);}));
        }
        private void Remember(EntityId id){ActorSnapshot a;if(world!=null&&world.TryGetActor(id,out a))positions[id.Value]=a.Position;}
        private bool Position(EntityId id,out WorldPoint p){Remember(id);return positions.TryGetValue(id.Value,out p);}
        private void Show(string text,WorldPoint p,Color color)
        {
            if(canvas==null||Camera.main==null)return;Vector3 screen=Camera.main.WorldToScreenPoint(new Vector3(p.X,p.Y,0));if(screen.z<0)return;Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform,screen,null,out local);
            GameObject o=new GameObject("Floating Text",typeof(RectTransform),typeof(Text));o.transform.SetParent(canvas.transform,false);RectTransform r=o.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=new Vector2(0.5f,0.5f);r.anchoredPosition=local;r.sizeDelta=new Vector2(170,32);
            Text t=o.GetComponent<Text>();t.font=font;t.fontSize=20;t.alignment=TextAnchor.MiddleCenter;t.color=color;t.text=text;t.raycastTarget=false;floating.Add(new Floating {Text=t,Origin=local,Life=0});
        }
        private void Play(AudioClip clip){if(clip!=null&&sound!=null)sound.PlayOneShot(clip,1);}
        private void Update()
        {
            for(int i=floating.Count-1;i>=0;i--){Floating f=floating[i];f.Life+=Time.unscaledDeltaTime;if(f.Text==null||f.Life>=0.9f){if(f.Text!=null)PresentationObjects.Release(f.Text.gameObject);floating.RemoveAt(i);continue;}f.Text.rectTransform.anchoredPosition=f.Origin+new Vector2(0,f.Life*34);Color c=f.Text.color;c.a=1-f.Life/0.9f;f.Text.color=c;}
        }
        public void SetMasterVolume(float value){volume=Mathf.Clamp01(value);if(sound!=null)sound.volume=volume;}
        public void Unbind(){foreach(IDisposable s in subscriptions)s.Dispose();subscriptions.Clear();world=null;positions.Clear();floating.Clear();if(canvas!=null)PresentationObjects.Release(canvas.gameObject);canvas=null;if(sound!=null)sound.Stop();}
        private void OnDestroy(){Unbind();}
    }
}

