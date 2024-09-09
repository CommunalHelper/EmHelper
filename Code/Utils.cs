using Celeste.Mod.EmHelper.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.EmHelper {
    public static class Utils {
        public static void ToggleMonumentActivators(this Entity entity, Color index) {
            foreach (MonumentActivator activator in entity.Scene.Tracker.GetComponents<MonumentActivator>()) {
                activator.Toggle(index);
            }
        }
    }

    public static class Extensions {
        public static Rectangle GetBounds(this Camera camera) {
            int top = (int)camera.Top;
            int bottom = (int)camera.Bottom;
            int left = (int)camera.Left;
            int right = (int)camera.Right;

            return new(left, top, right - left, bottom - top);
        }

        public static void DrawIfInRect(this MTexture mTexture, Rectangle rect, Vector2 pos, Vector2 origin, Color color) {
            if (rect.Contains(new Rectangle((int)pos.X - (int)origin.X, (int)pos.Y - (int)origin.Y, mTexture.Width, mTexture.Height)))
                mTexture.Draw(pos, origin, color);
        }
    }
}
