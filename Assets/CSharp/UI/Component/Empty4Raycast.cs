/**
 * @file            Empty4Raycast.cs
 * @author          fucj
 * @created         2021-03-09 11:22:17
 * @updated         2021-03-09 11:22:17
 *
 * @brief           
 */

namespace UnityEngine.UI
{
    [DisallowMultipleComponent]
    public sealed class Empty4Raycast : MaskableGraphic
    {
        protected override void Start()
        {
            base.Start();
            raycastTarget = true;
        }

        public override void SetAllDirty() { }
        public override void SetLayoutDirty() { }
        public override void SetNativeSize() { }
        public override void SetMaterialDirty() { }
        public override void SetVerticesDirty() { }
        public override void Rebuild(CanvasUpdate update)
        {
        }
    }
}
