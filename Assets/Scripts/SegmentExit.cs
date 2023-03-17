using LocalDirection = Direction.LocalDirection;
using GlobalDirection = Direction.GlobalDirection;
using DirectionConversion = Direction.DirectionConversion;
using Debug = UnityEngine.Debug;

namespace Segment {
    public class SegmentExit {
        private int x;
        private int z;
        private int y;
        private GlobalDirection direction;

        public SegmentExit(int entryX, int entryZ, int entryY, GlobalDirection gDirection, int forward, int right, int down, LocalDirection lDirection) {
            direction = DirectionConversion.GetDirection(gDirection, lDirection);
            y = entryY + down;
            switch (gDirection) {
                case GlobalDirection.North: {
                    x = entryX + forward;
                    z = entryZ + right;
                    break;
                }
                case GlobalDirection.East: {
                    x = entryX - right;
                    z = entryZ + forward;
                    break;
                }
                case GlobalDirection.South: {
                    x = entryX - forward;
                    z = entryZ - right;
                    break;
                }
                case GlobalDirection.West: {
                    x = entryX + right;
                    z = entryZ - forward;
                    break;
                }
            }
        }
        public SegmentExit(int x, int z, int y, GlobalDirection gDirection) {
            this.x = x;
            this.z = z;
            this.y = y;
            direction = gDirection;
        }
        public int X {
            get {
                return x;
            }
        }

        public int Z {
            get {
                return z;
            }
        }
        public int Y {
            get {
                return y;
            }
        }

        public GlobalDirection Direction {
            get {
                return direction;
            }
        }

        public override string ToString(){
            return "(" + x + ", " + z + ", " + y + " ) Gdirection: " + direction;
        } 
    }
}
