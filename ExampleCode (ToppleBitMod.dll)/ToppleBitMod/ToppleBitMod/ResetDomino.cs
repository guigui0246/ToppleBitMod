using System;
using System.Collections.Generic;
using ToppleBitModding;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ToppleBitMod
{
    internal class ResetDomino: Domino
    {
        private Rotation originalRotation;

        public static ResetDomino Create(Map map, Simulator simulator, Vector2Int position, Rotation rotation, FallState fallState, RotationSet hitRotations, Rotation originalRotation)
        {
            return new ResetDomino(map, simulator, position, rotation, fallState, hitRotations, originalRotation);
        }

        private ResetDomino(Map map, Simulator simulator, Vector2Int position, Rotation rotation, FallState fallState, RotationSet hitRotations, Rotation originalRotation)
            : base(map, simulator, position, rotation, fallState, hitRotations)
        {
            this.originalRotation = ((fallState == FallState.Standing) ? rotation : originalRotation);
        }

        public override void ChangeTick()
        {
            bool reset = false;
            base.ChangeTick();
            Rotation newRotation = hitRotations.GetSingleRotation();
            if (fallState == FallState.Falling)
            {
                if (newRotation != Rotation.None)
                {
                    if (newRotation.IsHoriztontal() && rotation.IsHoriztontal())
                    {
                        reset = true;
                    }
                    if (!newRotation.IsHoriztontal() && !rotation.IsHoriztontal())
                    {
                        reset = true;
                    }
                    rotation = newRotation;
                }
                Loader.Log($"[ResetDomino] Topple {newRotation.IsHoriztontal()}!");
            }
            else if (fallState == FallState.Unfalling)
            {
                rotation = newRotation;
            }
            if (reset)
            {
                Loader.Log("Reest happening");
                simulator.ResetSimulation();
            }
        }

        protected override RotationSet GetFallRotations()
        {
            if (hitRotations == RotationSet.Right)
            {
                return RotationSet.Right;
            }
            if (hitRotations == RotationSet.Up)
            {
                return RotationSet.Up;
            }
            if (hitRotations == RotationSet.Left)
            {
                return RotationSet.Left;
            }
            if (hitRotations == RotationSet.Down)
            {
                return RotationSet.Down;
            }
            if (hitRotations == RotationSet.RightUp)
            {
                if (!rotation.IsHoriztontal())
                {
                    return RotationSet.Up;
                }
                return RotationSet.Right;
            }
            if (hitRotations == RotationSet.RightLeft)
            {
                return RotationSet.None;
            }
            if (hitRotations == RotationSet.RightDown)
            {
                if (!rotation.IsHoriztontal())
                {
                    return RotationSet.Down;
                }
                return RotationSet.Right;
            }
            if (hitRotations == RotationSet.UpLeft)
            {
                if (!rotation.IsHoriztontal())
                {
                    return RotationSet.Up;
                }
                return RotationSet.Left;
            }
            if (hitRotations == RotationSet.UpDown)
            {
                return RotationSet.None;
            }
            if (hitRotations == RotationSet.LeftDown)
            {
                if (!rotation.IsHoriztontal())
                {
                    return RotationSet.Down;
                }
                return RotationSet.Left;
            }
            if (hitRotations == RotationSet.RightUpLeft)
            {
                return RotationSet.Up;
            }
            if (hitRotations == RotationSet.RightUpDown)
            {
                return RotationSet.Right;
            }
            if (hitRotations == RotationSet.RightLeftDown)
            {
                return RotationSet.Down;
            }
            if (hitRotations == RotationSet.UpLeftDown)
            {
                return RotationSet.Left;
            }
            _ = hitRotations == RotationSet.RightUpDownLeft;
            return RotationSet.None;
        }

        public override void Reset()
        {
            base.Reset();
            rotation = originalRotation;
        }

        public override void RotateLeft()
        {
            base.RotateLeft();
            originalRotation.RotateLeft();
        }

        public override void RotateRight()
        {
            base.RotateRight();
            originalRotation.RotateRight();
        }

        public override void MirrorX()
        {
            base.MirrorX();
            originalRotation.MirrorX();
        }

        public override void MirrorY()
        {
            base.MirrorY();
            originalRotation.MirrorY();
        }

        public override bool IsEquivalentTo(MapObject mapObject)
        {
            if (base.IsEquivalentTo(mapObject) && mapObject is ResetDomino domino)
            {
                return domino.originalRotation == originalRotation;
            }
            return false;
        }

        public override MapObject Clone()
        {
            return Create(map, simulator, position, rotation, fallState, hitRotations, originalRotation);
        }

        public override int GetGraphicIndex()
        {
            switch (fallState)
            {
                case FallState.Standing:
                    if (rotation == Rotation.Right)
                    {
                        return 0;
                    }
                    if (rotation == Rotation.Up)
                    {
                        return 1;
                    }
                    if (rotation == Rotation.Left)
                    {
                        return 2;
                    }
                    if (rotation == Rotation.Down)
                    {
                        return 3;
                    }
                    break;
                case FallState.Falling:
                    if (rotation == Rotation.Right)
                    {
                        return 4;
                    }
                    if (rotation == Rotation.Up)
                    {
                        return 5;
                    }
                    if (rotation == Rotation.Left)
                    {
                        return 6;
                    }
                    if (rotation == Rotation.Down)
                    {
                        return 7;
                    }
                    break;
                case FallState.Fallen:
                    if (rotation == Rotation.Right)
                    {
                        return 8;
                    }
                    if (rotation == Rotation.Up)
                    {
                        return 9;
                    }
                    if (rotation == Rotation.Left)
                    {
                        return 10;
                    }
                    if (rotation == Rotation.Down)
                    {
                        return 11;
                    }
                    break;
                case FallState.Unfalling:
                    if (rotation == Rotation.Right)
                    {
                        return 12;
                    }
                    if (rotation == Rotation.Up)
                    {
                        return 13;
                    }
                    if (rotation == Rotation.Left)
                    {
                        return 14;
                    }
                    if (rotation == Rotation.Down)
                    {
                        return 15;
                    }
                    break;
            }
            return -1;
        }

        public override Rotation GetSaveRotation()
        {
            return originalRotation;
        }
    }
}
