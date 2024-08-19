using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace NGUInjector.AllocationProfiles.BreakpointTypes
{
    internal class WandoosBP : BaseBreakpoint
    {
        protected override bool Unlocked()
        {
            return Character.buttons.wandoos.interactable && !Character.wandoos98.disabled;
        }

        protected override bool TargetMet()
        {
            return false;
        }

        internal override bool Allocate()
        {
            if (Type == ResourceType.Energy)
            {
                AllocateEnergy();
            }
            else
            {
                AllocateMagic();
            }

            return true;
        }

        private void AllocateEnergy()
        {
            //This is BUGGED, sometimes returns negative numbers due to dividing big numbers resulting in datatype overflow
            //long num1 = Character.wandoos98Controller.capAmountEnergy();
            //long num2 = (long)(num1 / (long)Math.Ceiling(num1 / (double)MaxAllocation) * 1.00000202655792);

            var num1 = ((double)Character.wandoos98Controller.baseEnergyTime() / Character.totalWandoosEnergySpeed()) + 1;
            var num2 = (long)(num1 / (long)Math.Ceiling(num1 / (double)MaxAllocation) * 1.000002f);

            //Can't allocate more than the breakpoint's configued max
            if (num2 > MaxAllocation)
            {
                SetInput(MaxAllocation);
            }
            //...or less than 0
            else if (num2 < 0)
            {
                SetInput(0);
            }
            else
            {
                SetInput(num2);
            }

            Character.wandoos98Controller.addEnergy();
        }

        private void AllocateMagic()
        {
            //This is BUGGED, sometimes returns negative numbers due to dividing big numbers resulting in datatype overflow
            //long num1 = Character.wandoos98Controller.capAmountMagic();
            //long num2 = (long)(num1 / (long)Math.Ceiling(num1 / (double)MaxAllocation) * 1.00000202655792);

            var num1 = ((double)Character.wandoos98Controller.baseMagicTime() / Character.totalWandoosMagicSpeed()) + 1;
            var num2 = (long)(num1 / (long)Math.Ceiling(num1 / (double)MaxAllocation) * 1.000002f);

            //Can't allocate more than the breakpoint's configued max
            if (num2 > MaxAllocation)
            {
                SetInput(MaxAllocation);
            }
            //...or less than 0
            else if (num2 < 0)
            {
                SetInput(0);
            }
            else
            {
                SetInput(num2);
            }

            Character.wandoos98Controller.addMagic();
        }

        protected override bool CorrectResourceType()
        {
            return Type == ResourceType.Energy || Type == ResourceType.Magic;
        }
    }
}
