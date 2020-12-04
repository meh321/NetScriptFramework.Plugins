using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetScriptFramework.SkyrimSE;

namespace IFPV.States
{
    class SpecialFurniture : CameraState
    {
        internal override int Priority
        {
            get
            {
                return (int)Priorities.SpecialFurniture;
            }
        }

        internal override void OnEntering(CameraUpdate update)
        {
            base.OnEntering(update);

            update.Values.FaceCamera.AddModifier(this, CameraValueModifier.ModifierTypes.Set, 0);
            Default.CantAutoTurnCounter++;
            update.Values.NearClip.AddModifier(this, CameraValueModifier.ModifierTypes.SetIfPreviousIsHigherThanThis, 3.0);
            update.Values.RotationFromHead.AddModifier(this, CameraValueModifier.ModifierTypes.SetIfPreviousIsLowerThanThis, 0.5);
        }

        internal override void OnLeaving(CameraUpdate update)
        {
            base.OnLeaving(update);

            Default.CantAutoTurnCounter--;
        }

        internal override bool Check(CameraUpdate update)
        {
            if (!update.CameraMain.IsEnabled)
                return false;

            var actor = update.Target.Actor;
            if (actor == null)
                return false;

            var process = actor.Process;
            if (process == null)
                return false;

            var middleHigh = process.MiddleHigh;
            if (middleHigh == null)
                return false;

            uint handle = middleHigh.CurrentFurnitureRefHandle;
            if (handle == 0)
                return false;

            TESObjectREFR obj = null;
            using (var objHandle = new ObjectRefHolder(handle))
            {
                if(objHandle.IsValid)
                    obj = objHandle.Object;
            }

            if (obj == null)
                return false;

            var baseObj = obj.BaseForm;
            if (baseObj == null)
                return false;

            foreach(var x in SpecialKeywords)
            {
                if (baseObj.HasKeywordText(x))
                    return true;
            }

            return false;
        }

        private static readonly string[] SpecialKeywords = new string[]
        {
            // Mining
            "isPickaxeTable",
            "isPickaxeWall",
            "isPickaxeFloor",

            // Other objects
            "FurnitureWoodChoppingBlock",
            "FurnitureResourceObjectSawmill",
            "isCartTravelPlayer",
        };
    }
}
