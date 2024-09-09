using Celeste.Mod.Entities;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;

namespace Celeste.Mod.EmHelper.Entities {
    [Tracked]
    [CustomEntity("EmHelper/Monumentswapblock")]
    public class MonumentSwapBlock : Solid {
        private readonly MonumentActivator activator;
        public MonumentSwapBlock(Vector2 position, float width, float height, Vector2 node, int pattern, Color color, bool mute)
            : base(position, width, height, false) {
            this.mute = mute;
            this.pattern = pattern;
            start = Position;
            end = node;
            maxForwardSpeed = 360f / Vector2.Distance(start, end);
            direction.X = Math.Sign(end.X - start.X);
            direction.Y = Math.Sign(end.Y - start.Y);
            int num = (int)MathHelper.Min(X, node.X);
            int num2 = (int)MathHelper.Min(Y, node.Y);
            int num3 = (int)MathHelper.Max(X + Width, node.X + Width);
            int num4 = (int)MathHelper.Max(Y + Height, node.Y + Height);
            moveRect = new Rectangle(num, num2, num3 - num, num4 - num2);
            MTexture mtexture;
            MTexture mtexture3;

            List<MTexture> atlasSubtextures = GFX.Game.GetAtlasSubtextures("objects/monumentswapblock/block");
            mtexture = atlasSubtextures[this.pattern % atlasSubtextures.Count];
            mtexture3 = GFX.Game["objects/monumentswapblock/target"];

            nineSliceGreen = new MTexture[3, 3];
            nineSliceTarget = new MTexture[3, 3];
            for (int i = 0; i < 3; i++) {
                for (int j = 0; j < 3; j++) {
                    nineSliceGreen[i, j] = mtexture.GetSubtexture(new Rectangle(i * 8, j * 8, 8, 8));
                    nineSliceTarget[i, j] = mtexture3.GetSubtexture(new Rectangle(i * 8, j * 8, 8, 8));
                }
            }

            Add(new LightOcclude(0.2f));
            Depth = -9999;
            SurfaceSoundIndex = 35;

            activator = new MonumentActivator(color, false, (activated) => Swap());
            Add(activator);
        }

        public MonumentSwapBlock(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Width, data.Height, data.Nodes[0] + offset, data.Int("pattern", 0), data.HexColor("color", Calc.HexToColor("82d9ff")), data.Bool("mute", false)) {
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            scene.Add(new PathRenderer(this));
        }

        public override void Removed(Scene scene) {
            base.Removed(scene);
            Audio.Stop(moveSfx, true);
        }

        public override void SceneEnd(Scene scene) {
            base.SceneEnd(scene);
            Audio.Stop(moveSfx, true);
        }

        public void Swap() {
            swapping = true;
            target = 1 - target;
            float relativeLerp = target == 1 ? lerp : 1 - lerp;
            speed = relativeLerp >= 0.2f ? maxForwardSpeed : MathHelper.Lerp(maxForwardSpeed * 0.333f, maxForwardSpeed, relativeLerp / 0.2f);
            Audio.Stop(moveSfx);
            if (!mute) {
                moveSfx = Audio.Play("event:/game/05_mirror_temple/swapblock_move", Center);
            }
        }

        public override void Update() {
            base.Update();
            speed = Calc.Approach(speed, maxForwardSpeed, maxForwardSpeed / 0.2f * Engine.DeltaTime);
            float num = lerp;
            lerp = Calc.Approach(lerp, target, speed * Engine.DeltaTime);
            if (lerp is 0 or 1) {
                Audio.Stop(moveSfx);
            }

            if (lerp != num) {
                Vector2 liftSpeed = (end - start) * speed;
                Vector2 position = Position;
                if (target == 1) {
                    liftSpeed = (end - start) * maxForwardSpeed;
                }

                if (lerp < num) {
                    liftSpeed *= -1f;
                }

                if (Scene.OnInterval(0.02f)) {
                    // Allows move particles in both directions
                    MoveParticles((end - start) * (target - 0.5f) * 2);
                }

                Vector2 to = Vector2.Lerp(start, end, lerp);
                Vector2 diff = to - ExactPosition;
                MoveH(diff.X, liftSpeed.X);
                MoveV(diff.Y, liftSpeed.Y);
                if (position != Position) {
                    Audio.Position(moveSfx, Center);
                    if (Position == start || Position == end) {
                        Audio.Stop(moveSfx);
                        if (!mute) {
                            Audio.Play("event:/game/05_mirror_temple/swapblock_move_end", Center);
                        }
                    }
                }
            }

            if (swapping && lerp >= 1f) {
                swapping = false;
            }

            StopPlayerRunIntoAnimation = lerp is <= 0f or >= 1f;
        }

        private void MoveParticles(Vector2 normal) {
            Vector2 position;
            Vector2 vector;
            float direction;
            float num;
            if (normal.X > 0f) {
                position = CenterLeft;
                vector = Vector2.UnitY * (Height - 6f);
                direction = 3.14159274f;
                num = Math.Max(2f, Height / 14f);
            } else if (normal.X < 0f) {
                position = CenterRight;
                vector = Vector2.UnitY * (Height - 6f);
                direction = 0f;
                num = Math.Max(2f, Height / 14f);
            } else if (normal.Y > 0f) {
                position = TopCenter;
                vector = Vector2.UnitX * (Width - 6f);
                direction = -1.57079637f;
                num = Math.Max(2f, Width / 14f);
            } else {
                position = BottomCenter;
                vector = Vector2.UnitX * (Width - 6f);
                direction = 1.57079637f;
                num = Math.Max(2f, Width / 14f);
            }

            particlesRemainder += num;
            int num2 = (int)particlesRemainder;
            particlesRemainder -= num2;
            vector *= 0.5f;
            SceneAs<Level>().Particles.Emit(SwapBlock.P_Move, num2, position, vector, direction);
        }

        public override void Render() {
            Vector2 vector = Position + Shake;
            if (lerp != target && speed > 0f) {
                Vector2 value = (end - start).SafeNormalize();
                if (target == 1) {
                    value *= -1f;
                }

                float num = speed / maxForwardSpeed;
                float num2 = 16f * num;
                int num3 = 2;
                while (num3 < num2) {
                    DrawBlockStyle(SceneAs<Level>().Camera, vector + (value * num3), Width, Height, nineSliceGreen, activator.Index * (1f - (num3 / num2)));
                    num3 += 2;
                }
            }

            DrawBlockStyle(SceneAs<Level>().Camera, vector, Width, Height, nineSliceGreen, activator.Index);
        }

        private static void DrawBlockStyle(Camera camera, Vector2 pos, float width, float height, MTexture[,] ninSlice, Color color) {
            Rectangle cameraBounds = camera.GetBounds();
            if (pos.X + width < cameraBounds.Left || pos.Y + height < cameraBounds.Top || pos.X > cameraBounds.Right || pos.Y > cameraBounds.Bottom)
                return;

            cameraBounds.Inflate(ninSlice[0, 0].Width, ninSlice[0, 0].Height);

            int tilesX = (int)(width / 8f);
            int tilesY = (int)(height / 8f);
            ninSlice[0, 0].DrawIfInRect(cameraBounds, pos + new Vector2(0f, 0f), Vector2.Zero, color);
            ninSlice[2, 0].DrawIfInRect(cameraBounds, pos + new Vector2(width - 8f, 0f), Vector2.Zero, color);
            ninSlice[0, 2].DrawIfInRect(cameraBounds, pos + new Vector2(0f, height - 8f), Vector2.Zero, color);
            ninSlice[2, 2].DrawIfInRect(cameraBounds, pos + new Vector2(width - 8f, height - 8f), Vector2.Zero, color);
            for (int i = 1; i < tilesX - 1; i++) {
                ninSlice[1, 0].DrawIfInRect(cameraBounds, pos + new Vector2(i * 8, 0f), Vector2.Zero, color);
                ninSlice[1, 2].DrawIfInRect(cameraBounds, pos + new Vector2(i * 8, height - 8f), Vector2.Zero, color);
            }
            for (int i = 1; i < tilesY - 1; i++) {
                ninSlice[0, 1].DrawIfInRect(cameraBounds, pos + new Vector2(0f, i * 8), Vector2.Zero, color);
                ninSlice[2, 1].DrawIfInRect(cameraBounds, pos + new Vector2(width - 8f, i * 8), Vector2.Zero, color);
            }
            for (int i = 1; i < tilesX - 1; i++) {
                for (int j = 1; j < tilesY - 1; j++) {
                    ninSlice[1, 1].DrawIfInRect(cameraBounds, pos + (new Vector2(i, j) * 8f), Vector2.Zero, color);
                }
            }
        }

        private readonly bool mute = false;

        private readonly int pattern;

        private Vector2 direction;

        private bool swapping;

        private Vector2 start;

        private Vector2 end;

        private float lerp;

        private int target;

        private Rectangle moveRect;

        private float speed;

        private readonly float maxForwardSpeed;

        private readonly MTexture[,] nineSliceGreen;

        private readonly MTexture[,] nineSliceTarget;

        private EventInstance moveSfx;

        private float particlesRemainder;

        private class PathRenderer : Entity {
            public PathRenderer(MonumentSwapBlock block)
                : base(block.Position) {
                this.block = block;
                Depth = 8999;
                timer = Calc.Random.NextFloat();
            }

            public override void Update() {
                base.Update();
                timer += Engine.DeltaTime * 4f;
            }

            public override void Render() {
                float alpha = 0.5f * (0.5f + (((float)Math.Sin(timer) + 1f) * 0.25f));
                DrawBlockStyle(SceneAs<Level>().Camera, new Vector2(block.moveRect.X, block.moveRect.Y), block.moveRect.Width, block.moveRect.Height, block.nineSliceTarget, block.activator.Index * alpha);
            }

            private readonly MonumentSwapBlock block;

            private float timer;
        }
    }
}