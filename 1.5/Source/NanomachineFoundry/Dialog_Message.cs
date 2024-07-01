using System;
using UnityEngine;
using Verse;

namespace NanomachineFoundry
{
    public class Dialog_Message : Window
    {
        public override Vector2 InitialSize => new Vector2(640f, 460f);

        
        private string contents;
        private Action onClick;
        private static readonly Vector2 ButtonSize = new Vector2(120f, 32f);

        public Dialog_Message(string contents, Action onClick = null)
        {
            this.onClick = onClick;
            this.contents = contents;
            forcePause = true;
            closeOnAccept = false;
            closeOnCancel = false;
            closeOnClickedOutside = false;
            absorbInputAroundWindow = true;
        }


        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Small;
            var flag = false;
            if (Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Return ||
                                                            Event.current.keyCode == KeyCode.KeypadEnter))
            {
                flag = true;
                Event.current.Use();
            }

            Rect rect1 = inRect with
            {
                width = (float)(inRect.width / 2.0 - 5.0),
                yMin = (float)(inRect.yMax - ButtonSize.y - 10.0)
            };
            Rect rect2 = inRect with
            {
                xMin = rect1.xMax + 10f,
                yMin = (float)(inRect.yMax - ButtonSize.y - 10.0)
            };
            Rect rect3 = inRect;
            rect3.y += 4f;
            rect3.yMax = rect2.y - 10f;
            using (new TextBlock(TextAnchor.UpperCenter))
            {
                Widgets.Label(rect3, contents);
            }

            if (!(Widgets.ButtonText(rect2, "Confirm".Translate()) | flag))
                return;
            onClick?.Invoke();
            Find.WindowStack.TryRemove(this);
        }
    }
}