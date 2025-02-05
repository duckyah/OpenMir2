﻿using GameSrv.Actor;

namespace GameSrv.Event.Events
{
    /// <summary>
    /// 火墙
    /// </summary>
    public class FireBurnEvent : MapEvent
    {
        /// <summary>
        /// 火墙运行时间
        /// </summary>
        protected int FireRunTick;
        private IList<BaseObject> ObjectList = new List<BaseObject>();

        public FireBurnEvent(BaseObject creat, short nX, short nY, byte nType, int nTime, int nDamage) : base(creat.Envir, nX, nY, nType, nTime, true)
        {
            Damage = nDamage;
            OwnBaseObject = creat;
        }

        public override void Run()
        {
            if ((HUtil32.GetTickCount() - FireRunTick) > 3000)
            {
                FireRunTick = HUtil32.GetTickCount();
                if (Envirnoment != null)
                {
                    Envirnoment.GetBaseObjects(nX, nY, true, ref ObjectList);
                    for (int i = 0; i < ObjectList.Count; i++)
                    {
                        BaseObject targetBaseObject = ObjectList[i];
                        if (targetBaseObject != null && OwnBaseObject != null && OwnBaseObject.IsProperTarget(targetBaseObject))
                        {
                            targetBaseObject.SendMsg(OwnBaseObject, Messages.RM_MAGSTRUCK_MINE, 0, Damage, 0, 0);
                        }
                    }
                }
                ObjectList.Clear();
            }
            base.Run();
        }
    }
}