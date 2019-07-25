using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        //declaring the command list
        Dictionary<string, Action> commands;

        //stuff required for the ini
        MyIni ini;
        String limitSection = "Set your limits here";
        String stateSection = "Current State of the Machine";

        //declaring the limits and values that are not going to change
        //these will be set by the ini file in the Custom Data field of the Programmable Block
        byte sizeDrillhead;
        int depthLimit;
        int maxRadiusLimit;
        int minRadiusLimit;
        float lowerAngleLimit;
        float upperAngleLimit;

        //declaring the lists of blocks the drillarm consists of
        List<IMyPistonBase> armPiston;
        List<IMyPistonBase> chestPiston;
        List<IMyPistonBase> drillPiston;
        List<IMyShipDrill> drillHead;
        IMyMotorStator rotor;

        //declaring the values that will change all the time
        //those are also the only values that need to be stored in the Storage String
        Boolean direction;
        Boolean isPaused;
        byte state;
        float angle;
        float radius;

        //called whenever the program is recompiled and run or the game loads the save again
        public Program()
        {
            //setting the commands
            commands = new Dictionary<string, Action>(StringComparer.OrdinalIgnoreCase);
            commands["man/auto"] = ManAuto;
            commands["continue"] = Continue;
            commands["pause"] = Pause;
            commands["restart"] = Restart;
            commands["test"] = Test;

            ini = new MyIni();

            //filling the lists that represent the drillarm with blocks from the grid
            armPiston = fillPistonList("armpistons");
            chestPiston = fillPistonList("chestpistons");
            drillPiston = fillPistonList("drillpistons");
            drillHead = fillDrillList("drillhead");
            rotor = (IMyMotorStator)GridTerminalSystem.GetBlockWithName("drillrotor");

            //filling the variables from storage
            if (Storage != null && !Storage.Equals(""))
            {
                try
                {
                    //TODO redo this
                    string[] loaded = Storage.Split(';');
                    Boolean.TryParse(loaded[0], out direction);
                    Boolean.TryParse(loaded[1], out isPaused);
                    byte.TryParse(loaded[2], out state);
                    float.TryParse(loaded[3], out angle);
                    float.TryParse(loaded[4], out radius);
                }
                catch (Exception e)
                {
                    Echo(e.StackTrace);
                }
            }
            else
            {
                //setting default values if the storage string is empty
                direction = true;
                isPaused = true;
                state = 0;
                angle = rotor.Angle;
                radius = calculateRadius();
            }

            if (isPaused)
            {
                //if the script starts in the paused state
                settingDrillarmEnabled(false);

                printIniToCustomData();

                Runtime.UpdateFrequency = UpdateFrequency.None;

                Echo("- Paused -\n\nFeel free to edit the configurations in \"Custom Data\"");
            }
            else
            {
                //Script is fired every 100 ticks / 1,6s
                Runtime.UpdateFrequency = UpdateFrequency.Update100;
            }



            /*
            //checking some values
            for (int i = 0; i < armPiston.Count; i++)
            {
                float force = armPiston[i].GetValue<float>("MaxImpulseAxis");
                if (force != 100000) armPiston[i].SetValue<float>("MaxImpulseAxis", 100000);
                force = armPiston[i].GetValue<float>("MaxImpulseNonAxis");
                if (force != 100000) armPiston[i].SetValue<float>("MaxImpulseNonAxis", 100000);
            }

            for (int i = 0; i < chestPiston.Count; i++)
            {
                float force = chestPiston[i].GetValue<float>("MaxImpulseAxis");
                if (force != 100000) chestPiston[i].SetValue<float>("MaxImpulseAxis", 100000);
                force = chestPiston[i].GetValue<float>("MaxImpulseNonAxis");
                if (force != 100000) chestPiston[i].SetValue<float>("MaxImpulseNonAxis", 100000);
            }

            for (int i = 0; i < drillPiston.Count; i++)
            {
                float force = drillPiston[i].GetValue<float>("MaxImpulseAxis");
                if (force != 100000) drillPiston[i].SetValue<float>("MaxImpulseAxis", 100000);
                force = drillPiston[i].GetValue<float>("MaxImpulseNonAxis");
                if (force != 100000) drillPiston[i].SetValue<float>("MaxImpulseNonAxis", 100000);
            }       
            */
        }

        //saves the current state of the machine to the storage string
        public void Save()
        {
            //TODO redo this

            //saving the required stuff into the saving string, using a ; as a splitter
            Storage = direction + ";" +
                isPaused + ";" +
                state + ";" +
                angle + ";" +
                radius;
        }

        //searches for a group called the given name and returns a list with those blocks, converted to IMyPistonBase
        List<IMyPistonBase> fillPistonList(String nameOfGroup)
        {
            List<IMyTerminalBlock> temp = new List<IMyTerminalBlock>();
            List<IMyPistonBase> result = new List<IMyPistonBase>();

            //TODO add checks if this list is found
            GridTerminalSystem.GetBlockGroupWithName(nameOfGroup).GetBlocks(temp);

            for (int i = 0; i < temp.Count; i++)
            {
                result.Add((IMyPistonBase)temp[i]);
            }

            return result;

        }

        //searches for a group called the given name and returns a list with those blocks, converted to IMyShipDrill
        List<IMyShipDrill> fillDrillList(String nameOfGroup)
        {
            List<IMyTerminalBlock> temp = new List<IMyTerminalBlock>();
            List<IMyShipDrill> result = new List<IMyShipDrill>();

            //TODO add checks if this list is found
            GridTerminalSystem.GetBlockGroupWithName(nameOfGroup).GetBlocks(temp);

            for (int i = 0; i < temp.Count; i++)
            {
                result.Add((IMyShipDrill)temp[i]);
            }

            return result;
        }


        /* * * * * * * * * * * * * * * * * * * * *
         *  # Here the Commands are defined #    *
         * * * * * * * * * * * * * * * * * * * * */


        //switches the machine from manual to automatic
        void ManAuto()
        {
            if (!isPaused)
            {
                //if in manual it needs to switch to automatic mode, starting in state 1
                if (state == 0)
                {
                    //TODO check this
                    setValuesDrilling();
                    state = 1;
                }
                else
                {
                    //TODO check this
                    setValuesManual();
                    state = 0;
                }
            }
            else
            {
                Echo("The machine is still paused! You need to continue first.");
            }
        }


        //Continues the drillstation where it was last paused. This loads the ini file from the Custom Data field of the Programmable Block.
        void Continue()
        {
            if (isPaused)
            {
                MyIniParseResult result;
                //if the ini can not be parsed correctly an exception will be thrown
                if (!ini.TryParse(Me.CustomData, out result))
                {
                    Echo("Something went wrong when reading the Custom Data field!");
                    throw new Exception(result.ToString());
                }
                else
                {
                    //receiving all values from the ini with checks wether or not the user made a mistake
                    if (!ini.Get(limitSection, "sizeDrillHead").TryGetByte(out sizeDrillhead))
                    {
                        Echo("There was an error when trying to parse \"sizeDrillHead\"");
                        return;
                    }
                    if (!ini.Get(limitSection, "depthLimit").TryGetInt32(out depthLimit))
                    {
                        Echo("There was an error when trying to parse \"depthLimit\"");
                        return;
                    }
                    if (!ini.Get(limitSection, "maxRadiusLimit").TryGetInt32(out maxRadiusLimit))
                    {
                        Echo("There was an error when trying to parse \"maxRadiusLimit\"");
                        return;
                    }
                    if (!ini.Get(limitSection, "minRadiusLimit").TryGetInt32(out minRadiusLimit))
                    {
                        Echo("There was an error when trying to parse \"minRadiusLimit\"");
                        return;
                    }
                    if (!ini.Get(limitSection, "lowerAngleLimit").TryGetSingle(out lowerAngleLimit))
                    {
                        Echo("There was an error when trying to parse \"lowerAngleLimit\"");
                        return;
                    }
                    if (!ini.Get(limitSection, "upperAngleLimit").TryGetSingle(out upperAngleLimit))
                    {
                        Echo("There was an error when trying to parse \"upperAngleLimit\"");
                        return;
                    }

                    if (!ini.Get(stateSection, "direction").TryGetBoolean(out direction))
                    {
                        Echo("There was an error when trying to parse \"direction\"");
                        return;
                    }
                    if (!ini.Get(stateSection, "state").TryGetByte(out state))
                    {
                        Echo("There was an error when trying to parse \"state\"");
                        return;
                    }
                    if (!ini.Get(stateSection, "angle").TryGetSingle(out angle))
                    {
                        Echo("There was an error when trying to parse \"angle\"");
                        return;
                    }
                    if (!ini.Get(stateSection, "radius").TryGetSingle(out radius))
                    {
                        Echo("There was an error when trying to parse \"radius\"");
                        return;
                    }

                    Me.CustomData = "The machine is currently running. To edit your settings please pause the machine.";
                    settingDrillarmEnabled(true);
                    Runtime.UpdateFrequency = UpdateFrequency.Update100;
                    isPaused = false;
                }

            }
            else
            {
                Echo("The machine was not paused!");
            }
        }

        //This stops the drillstation, printing a fresh ini file into the Custom Data field of the Programmable Block.
        //This allows the user to change the values there if wanted.
        void Pause()
        {
            if (!isPaused)
            {
                isPaused = true;

                settingDrillarmEnabled(false);

                printIniToCustomData();

                Runtime.UpdateFrequency = UpdateFrequency.None;

                Echo("- Paused -\n\nFeel free to edit the configurations in \"Custom Data\"");
            }
            else
            {
                Echo("The machine is already paused!");
            }
        }

        //Loads the ini file from the Custom Data field of the Programmable Block. Positions the drill head in the left-closest position
        //inside the given limits and starts drilling.
        //This is meant to be run after setting new limits so that manual adjustment of the drillhead is avoided.
        void Restart()
        {
            //TODO Restart()
        }

        //just for testing stuff out
        void Test()
        {

        }


        /* * * * * * * * * * * * * * * * * * * * * * * * * * * * *
         *  # Helper methods that are needed for the commands #  *
         * * * * * * * * * * * * * * * * * * * * * * * * * * * * */


        //turning all the moving parts of the drillarm on or off
        void settingDrillarmEnabled(Boolean toSet)
        {
            rotor.Enabled = toSet;
            for (int i = 0; i < drillHead.Count; i++)
            {
                drillHead[i].Enabled = toSet;
            }
            for (int i = 0; i < chestPiston.Count; i++)
            {
                chestPiston[i].Enabled = toSet;
            }
            for (int i = 0; i < armPiston.Count; i++)
            {
                armPiston[i].Enabled = toSet;
            }
            for (int i = 0; i < drillPiston.Count; i++)
            {
                drillPiston[i].Enabled = toSet;
            }
        }

        void printIniToCustomData()
        {
            //loading the current state of the machine in the Custom Data field for editing.
            ini.Set(limitSection, "sizeDrillhead", sizeDrillhead);
            ini.Set(limitSection, "depthLimit", depthLimit);
            ini.Set(limitSection, "maxRadiusLimit", maxRadiusLimit);
            ini.Set(limitSection, "minRadiusLimit", minRadiusLimit);
            ini.Set(limitSection, "lowerAngleLimit", lowerAngleLimit);
            ini.Set(limitSection, "upperAngleLimit", upperAngleLimit);
            ini.SetSectionComment(limitSection, "This is the only section you should touch unless you *really* know what you are doing.");

            ini.Set(stateSection, "direction", direction);
            //ini.Set(stateSection, "isPaused", isPaused);  This would not make sense in the ini, as it will be set the very moment the ini is loaded
            ini.Set(stateSection, "state", state);
            ini.Set(stateSection, "angle", angle);
            ini.Set(stateSection, "radius", radius);
            ini.SetSectionComment(stateSection, "Do not touch this section unless you *really* know what you are doing.");

            Me.CustomData = ini.ToString();
        }


        /* * * * * * * * * * * * * * * * * * *
         *  # Here the States are defined #  *
         * * * * * * * * * * * * * * * * * * */

        //state 0, manual control of the station
        void Manual()
        {
            //TODO this
        }

        //sets the values that need to be set only once
        void setValuesManual()
        {
            //TODO this
        }

        //state 1, the drillarm is lowered and drills
        void Drilling()
        {
            //expanding the drillarm-pistons, one after the other
            for (int i = 0; i < drillPiston.Count; i++)
            {
                if (drillPiston[i].CurrentPosition != drillPiston[i].MaxLimit) return;
            }

            //contracting the chestarm-pistons, one after the other
            for (int i = 0; i < chestPiston.Count; i++)
            {
                if (chestPiston[i].CurrentPosition != chestPiston[i].MinLimit) return;
            }

            //once the pistons reached their limits the station enters the lifting-state
            setValuesLifting();
            state = 2;
        }

        //sets the values that need to be set only once
        void setValuesDrilling()
        {
            int remainingDepth = depthLimit;
            //setting the maxLimits and velocity of the drillpistons
            for (int i = 0; i < drillPiston.Count; i++)
            {
                if (remainingDepth >= 10)
                {
                    drillPiston[i].MaxLimit = 10;
                    drillPiston[i].Velocity = 0.5F;
                    remainingDepth -= 10;
                }
                else if (remainingDepth == 0)
                {
                    drillPiston[i].MaxLimit = 0;
                    drillPiston[i].Velocity = 0;
                }
                else
                {
                    drillPiston[i].MaxLimit = remainingDepth;
                    drillPiston[i].Velocity = 0.5F;
                    remainingDepth = 0;
                }
            }

            for (int i = 0; i < chestPiston.Count; i++)
            {
                if (remainingDepth >= 10)
                {
                    chestPiston[i].MinLimit = 0;
                    chestPiston[i].Velocity = -0.5F;
                    remainingDepth -= 10;
                }
                else if (remainingDepth == 0)
                {
                    chestPiston[i].MinLimit = 10;
                    chestPiston[i].Velocity = 0;
                }
                else
                {
                    chestPiston[i].MinLimit = 10 - remainingDepth;
                    chestPiston[i].Velocity = -0.5F;
                    remainingDepth = 0;
                }
            }
            //TODO: implement soft error message if the given depthlimit is deeper than the drills can actually drill
            if (remainingDepth > 0)
            {
                //throw the error message
            }

            //turning on the drills
            for (int i = 0; i < drillHead.Count; i++)
            {
                drillHead[i].SetValueBool("OnOff", true);
            }
        }

        //state 2, the drillarm is lifted and the drills are turnt off
        void Lifting()
        {
            //checking the pistons on the chestarm
            for (int i = 0; i < chestPiston.Count; i++)
            {
                if (chestPiston[i].CurrentPosition != 10F) return;
            }

            //checking the pistons of the drillarm
            for (int i = 0; i < drillPiston.Count; i++)
            {
                if (drillPiston[i].CurrentPosition != 0F) return;
            }

            //setting the velocity of chest pistons and drill pistons to 0 to avoid stressing the simulation
            for (int i = 0; i < chestPiston.Count; i++)
            {
                chestPiston[i].Velocity = 0;
            }

            for (int i = 0; i < drillPiston.Count; i++)
            {
                drillPiston[i].Velocity = 0;
            }


            //DECIDING WHAT STATE TO SWITCH TO

            if (direction)  //..turning clockwise
            {
                //if the angle is the limit go to stretching
                if (angle == upperAngleLimit)
                {
                    //if the current radius is the limit, the drillstation is done drilling
                    if (radius == maxRadiusLimit) state = 5;   //switch to the done-state
                    else
                    {   //...stretch
                        setValuesExtendingArmPistons();
                        direction ^= true;                  //inverting the direction boolean
                        state = 4;                          //switch to the stretch-state
                    }
                }
                else
                {   //.. turn once more
                    setValuesTurningClockwise();
                    state = 3;                              //switch to turning-state
                }
            }
            else        //..if turning anti-clockwise
            {
                //if the angle is the limit go to stretching
                if (angle == lowerAngleLimit)
                {
                    //if the current radius is the limit, the drillstation is done drilling
                    if (radius == maxRadiusLimit) state = 5;   //switch to done-state
                    else
                    {   //...stretch
                        setValuesExtendingArmPistons();
                        direction ^= true;                  //inverting the direction boolean
                        state = 4;                          //switch to the stretch-state
                    }
                }
                else
                {   //...turn once more
                    setValuesTurningAntiClockwise();
                    state = 3;                              //switch to turning-state
                }
            }
        }

        //sets the values that need to be set only once
        void setValuesLifting()
        {
            //expanding the chestarm-pistons
            for (int i = 0; i < chestPiston.Count; i++)
            {
                chestPiston[i].Velocity = 0.5F;
            }

            //contracting the drillarm-pistons
            for (int i = 0; i < drillPiston.Count; i++)
            {
                drillPiston[i].Velocity = -0.5F;
            }

            //turning off the drills
            for (int i = 0; i < drillHead.Count; i++)
            {
                drillHead[i].SetValueBool("OnOff", false);
            }
        }

        //state 3, the drillarm is turning
        void Turning()
        {
            //check wether the rotor reached the desired angle yet
            if (MathHelper.ToDegrees(rotor.Angle) != angle) return;

            //stopping the rotation
            //this should be irrelevant as the rotor won't turn any further than the limits
            //but it's better not to stress things unnecessarily
            rotor.TargetVelocityRPM = 0;

            //after the turn is done, switch to drilling-state
            setValuesDrilling();
            state = 1;
        }

        //sets the values that need to be set only once
        void setValuesTurningClockwise()
        {
            float angleDelta = calculateAngleDelta();

            //if turning once more would surpass the limit, the limit is the new angle
            if (angle + angleDelta > upperAngleLimit) angle = upperAngleLimit;
            else angle += angleDelta;

            //setting the rotor accordingly
            rotor.UpperLimitDeg = angle;
            rotor.LowerLimitDeg = angle;
            rotor.TargetVelocityRPM = 1;        //positive value means turning clockwise
        }

        //sets the values that need to be set only once
        void setValuesTurningAntiClockwise()
        {
            float angleDelta = calculateAngleDelta();

            //if turning once more would surpass the limit, the limit is the new angle
            if (angle - angleDelta > lowerAngleLimit) angle = lowerAngleLimit;
            else angle -= angleDelta;

            //setting the rotor accordingly
            rotor.UpperLimitDeg = angle;
            rotor.LowerLimitDeg = angle;
            rotor.TargetVelocityRPM = -1;       //negative value means turning anti-clockwise
        }

        //state 4, the drillarm is stretching one layer further
        void Stretching()
        {
            //waiting for every piston to expand to the limit
            for (int i = 0; i < armPiston.Count; i++)
            {
                if (armPiston[i].CurrentPosition != armPiston[i].MaxLimit) return;
            }

            //once limits are reached, set velocity to 0 to avoid stressing the simulation
            for (int i = 0; i < armPiston.Count; i++)
            {
                armPiston[i].Velocity = 0;
            }

            //once stretching is over, enter state 1 again.
            setValuesDrilling();
            state = 1;
        }

        //sets the values that need to be set only once
        void setValuesExtendingArmPistons()
        {
            //distance the pistons need to extend;
            //the size of the drillhead plus 1m as the drilled holes are bigger than the drillhead
            float radiusDelta = sizeDrillhead * 2.5F + 1;

            //if stretching would exceed the limit, the limit is the new radius
            if (radius + radiusDelta > maxRadiusLimit) radius = maxRadiusLimit;
            else radius += radiusDelta;

            /* The total distance the pistons need to expand
             * Calculated by subtracting all the fixed parts and distances from the radius we want to reach
             * Every piston in the arm:             10m
             * half a pipe twice:                   2.5m
             * half the edge length of the drillhead
             * */
            float remainingExpansion = radius - (armPiston.Count * 10 + sizeDrillhead * 1.25F + 2.5F);

            //setting the maxLimits and velocity of the armpistons
            for (int i = 0; i < armPiston.Count; i++)
            {
                if (remainingExpansion >= 10)
                {
                    armPiston[i].MaxLimit = 10;
                    armPiston[i].Velocity = 0.5F;
                    remainingExpansion -= 10;
                }
                else if (remainingExpansion == 0)
                {
                    armPiston[i].MaxLimit = 0;
                    armPiston[i].Velocity = 0;
                }
                else
                {
                    armPiston[i].MaxLimit = remainingExpansion;
                    armPiston[i].Velocity = 0.5F;
                    remainingExpansion = 0;
                }
            }
        }

        //state 5, the machine is done, goes into a standby position and communicates this
        void Done()
        {
            if (rotor.Angle != 0) return;

            for (int i = 0; i < armPiston.Count; i++)
            {
                if (armPiston[i].CurrentPosition != 0) return;
            }

            //defaulting some values at the last run of the done-state
            //this is to avoid hindering manual manouvering of the station once drilling is done
            for (int i = 0; i < chestPiston.Count; i++)
            {
                chestPiston[i].MinLimit = 0;
            }
            for (int i = 0; i < armPiston.Count; i++)
            {
                armPiston[i].MaxLimit = 10;
            }
            for (int i = 0; i < drillPiston.Count; i++)
            {
                drillPiston[i].MaxLimit = 10;
            }

            //only after the station is done putting the whole drillarm back to a neutral position,
            //we turn off the programmable block
            Me.SetValueBool("OnOff", false);
        }

        //sets the values that need to be set only once
        void setValuesDone()
        {
            //TODO: make the antenna say "DONE" and maybe add a screen on a foreign grid that says "DONE"

            //the rotor shall turn into a neutral position
            rotor.LowerLimitDeg = 0;
            rotor.UpperLimitDeg = 0;

            if (rotor.Angle < 0) rotor.TargetVelocityRPM = 1;
            else if (rotor.Angle > 0) rotor.TargetVelocityRPM = -1;
            else rotor.TargetVelocityRPM = 0;

            //armpistons should retract completely
            //as this is only called after lifting() we dont need to lift the drillhead anymore
            for (int i = 0; i < armPiston.Count; i++)
            {
                armPiston[i].Velocity = -0.5F;
            }
        }

        //state 6, lifts turns and contracts the drillarm in the closest, most left position
        //within the given limits
        void Restarting()
        {
            //TODO this
        }

        //sets the values that need to be set only once
        void setValuesRestarting()
        {
            //TODO this
        }




        //returns the angle the rotor needs to turn to dig exactly next to the hole we just dug
        float calculateAngleDelta()
        {
            /*Formula:
             *          180 * arc
             * alpha = -----------
             *            r * pi
             *            
             * the arc is the size of the drillhead in meters + 1, as the arc should be a curve but we have straight lines.
             * furthermore, the drills dig slightly bigger holes than themselfs.
             */
            float angleDelta = (180 * (sizeDrillhead * 2.5F + 1)) /
                                 (radius * (float)(Math.PI));

            return angleDelta;
        }

        //returns the current radius
        float calculateRadius()
        {
            /*Half a block for each knee-tube                           = 2.5
             * every piston in the arm has 5m plus its current extend   = 5 * armPiston.Count
             * half of the edge length of the drillhead                 = sizeDrillhead * 1.25
             * */
            float currentRadius = 2.5F + 5F * armPiston.Count + sizeDrillhead * 1.25F;

            //adding the current extent of the pistons
            for (int i = 0; i < armPiston.Count; i++)
            {
                currentRadius += ((IMyPistonBase)armPiston[i]).CurrentPosition;
            }

            return currentRadius;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            //if we are given an argument
            if (argument != null && !argument.Equals(""))
            {
                Action commandAction;

                //true if we find a argument in our list
                if (commands.TryGetValue(argument, out commandAction))
                {
                    commandAction();
                }
                else   //if the argument was not recognized
                {
                    Echo("Given argument was not recognized!");
                }
            }
            else  //if no argument was given the script just does its thing
            {
                //if the machine is paused the state machine does not need to be accessed at all.
                if (!isPaused)
                {
                    /* 0  manual controls
                     * 1  drillhead is lowering
                     * 2  drillhead is lifting
                     * 3  the rotor is turning a little
                     * 4  armPistons are extending
                     * 5  done
                     * 6  restarting from the beginning
                     * default none
                     * */
                    switch (state)
                    {
                        case 1:
                            Drilling();
                            break;
                        case 2:
                            Lifting();
                            break;
                        case 3:
                            Turning();
                            break;
                        case 4:
                            Stretching();
                            break;
                        case 5:
                            Done();
                            break;
                        case 6:
                            Restarting();
                            break;
                        default:
                            break;
                    }
                }

                //print some information about the current state
                Echo("Current state: " + state);

                //ending the script with some performance feedback in the output area
                string time = Runtime.LastRunTimeMs.ToString("0.000000");
                Echo("Last runtime: " + time + "ms");

                string timeGap = Runtime.TimeSinceLastRun.ToString();
                Echo("Time since last run: " + timeGap + "ms");

                string instructions = Runtime.CurrentInstructionCount.ToString();
                Echo("Current InstructionCount: " + instructions);

                Echo("isPaused: " + isPaused);
            }
        }
    }
}