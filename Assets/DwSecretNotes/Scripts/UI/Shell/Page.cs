using UnityEngine.UIElements;
namespace DwSecretNotes.UI
{
    public abstract class Page
    {
        public VisualElement Root { get; private set; }
        protected AppShell Shell { get; private set; }
        public abstract string Route { get; }
        public virtual bool ShowBack => true;
        public virtual bool ShowTopBar => true;
        public virtual bool KeepAlive => false;
        public void Init(AppShell shell) { Shell = shell; Root = new VisualElement(); Root.AddToClassList("dw-page"); Build(Root); }
        protected abstract void Build(VisualElement root);
        public virtual void OnShow() { }
        public virtual void OnHide() { }
        public virtual void OnUpdate() { }
        public virtual bool HandleBack() => false;
        protected ScrollView Scroll(VisualElement root)
        {
            var sv = new ScrollView(ScrollViewMode.Vertical) { touchScrollBehavior = ScrollView.TouchScrollBehavior.Elastic, horizontalScrollerVisibility = ScrollerVisibility.Hidden, verticalScrollerVisibility = ScrollerVisibility.Hidden };
            sv.AddToClassList("dw-scroll"); sv.contentContainer.AddToClassList("dw-scroll__content"); Kit.CenterColumn(sv.contentContainer); root.Add(sv); return sv;
        }
    }
}
