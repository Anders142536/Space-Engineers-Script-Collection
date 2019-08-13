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
        /* TODO:
         * 
         * Whenever the ship weight changes, calculate the relative change and add that to the dampeners
         * MyShipMass
         * store connectors
         * write method that unlocks/locks connectors and checks ship mass and redoes dampeners
         * 
         * */
        IMyShipController controller;       //this determines the user input on the car
        List<IMyInteriorLight> brake;       //the group of brake lights
        List<IMyInteriorLight> reverse;     //the group of the reverse lights
        List<IMyReflectorLight> spotlights; //the group of the spotlights
        List<Wheel> wheels;
        List<IMyShipConnector> connectors;
        List<IMyTerminalBlock> tools;
        IMyRadioAntenna antenna;
        bool reverseManual;
        bool reverseOn;
        bool spotlightsOn;
        bool brakesOn;
        float maxMass;
        Color brakeOnColor;
        Color brakeOffColor;
        //IMyTextPanel output;  //This is only active when debugging

        /* Scripts running every tick (every 16,6 ms in theory) are frowned upon in the community for good reasons.
         * This script only takes 0.01 - 0.005 ms to do its job, which is basically nothing.
         * Feel free to change this value from Update1 to Update10, which will cause the script to run
         * every 10 ticks (every 166,6 ms in theory) and keep it very responsive.
         * For maximum responsiveness keep it at that value.
         * This only influences the script when someone is seated in the cockpit.
         * If no one is seated the script will run every 100 ticks (every 1,66s in theory) only to save performance.*/
        UpdateFrequency seated = UpdateFrequency.Update1;


        /* * * * *
         * SETUP *
         * * * * */

        //initializing everything that does not require the gridsystem
        //everything that requires the gridsystem is initialized whenever the script is run with UpdateType.Once
        public Program()
        {
            //only stuff that does not need the GridSystem is initialized here
            reverseOn = false;
            spotlightsOn = false;
            brakesOn = false;

            //TODO add this to ini
            reverseManual = false;
            brakeOnColor = new Color(255, 0, 0);
            brakeOffColor = new Color(5, 0, 0);

            //we need the .Once to initialize on the second tick
            Runtime.UpdateFrequency = seated | UpdateFrequency.Once;
        }

        private void init()
        {
            controller = getMainCockpit();
            brake = fillLightList("lights brake");
            reverse = fillLightList("lights reverse");
            spotlights = fillSpotlightList("spotlights");
            wheels = getWheels();
            connectors = getConnectors();
            antenna = getAntenna();
            //load some ini for wheel strength, max weight, color values
        }

        //searches for a group called the given name and returns a list with those blocks, converted to IMyInteriorLights
        private List<IMyInteriorLight> fillLightList(String nameOfGroup)
        {
            IMyBlockGroup tempG = GridTerminalSystem.GetBlockGroupWithName(nameOfGroup);

            if (tempG != null)  //if there is no group called like that
            {
                List<IMyInteriorLight> result = new List<IMyInteriorLight>();
                tempG.GetBlocksOfType(result);

                //check if there were blocks on type IMyInteriorLight found on the grid
                if (result.Count == 0) return null;
                return result;
            }
            return null;
        }

        //searches for a group called the given name and returns a list with those blocks, converted to IMySpotlights
        private List<IMyReflectorLight> fillSpotlightList(String nameOfGroup)
        {
            IMyBlockGroup tempG = GridTerminalSystem.GetBlockGroupWithName(nameOfGroup);

            if (tempG != null)  //if there is no group called like that
            {
                List<IMyReflectorLight> result = new List<IMyReflectorLight>();
                tempG.GetBlocksOfType(result);

                //check if there were blocks on type IMyReflectorLight found on the grid
                if (result.Count == 0) return null;
                return result;
            }
            return null;
        }

        //returns all wheel suspensions on the ship
        private List<Wheel> getWheels()
        {
            List<IMyMotorSuspension> temp = new List<IMyMotorSuspension>();
            List<Wheel> result = new List<Wheel>();
            GridTerminalSystem.GetBlocksOfType(temp);

            //if there are no wheels
            if (temp.Count == 0) return null;

            //fill the wheels list with objects of Wheel
            for (int i = 0; i < temp.Count; i++)
            {
                result.Add(new Wheel(temp[i], temp[i].CustomName));
            }
            return result;
        }

        //returns all connectors on the ship
        private List<IMyShipConnector> getConnectors()
        {
            List<IMyShipConnector> result = new List<IMyShipConnector>();
            GridTerminalSystem.GetBlocksOfType(result);

            //check if there were blocks on type IMyShipConnector found on the grid
            if (result.Count == 0) return null;
            return result;
        }

        //returns all welders, grinders and drills
        private List<IMyTerminalBlock> getTools()
        {
            List<IMyShipWelder> welders = new List<IMyShipWelder>();
            List<IMyShipGrinder> grinders = new List<IMyShipGrinder>();
            List<IMyShipDrill> drills = new List<IMyShipDrill>();
            List<IMyTerminalBlock> result = new List<IMyTerminalBlock>();

            GridTerminalSystem.GetBlocksOfType(welders);
            GridTerminalSystem.GetBlocksOfType(grinders);
            GridTerminalSystem.GetBlocksOfType(drills);

            //adding all found welders to the result list
            foreach (IMyShipWelder welder in welders)
            {
                result.Add((IMyTerminalBlock)welder);
            }

            //adding all found grinders to the result list
            foreach (IMyShipGrinder grinder in grinders)
            {
                result.Add((IMyTerminalBlock)grinder);
            }

            //adding all found drills to the result list
            foreach (IMyShipDrill drill in drills)
            {
                result.Add((IMyTerminalBlock)drill);
            }

            if (result.Count == 0) return null;
            return result;

        }

        //returns the main cockpit, if there is one
        private IMyShipController getMainCockpit()
        {
            List<IMyShipController> controllers = new List<IMyShipController>();
            GridTerminalSystem.GetBlocksOfType(controllers);

            if (controllers.Count == 0) return null;
            if (controllers.Count == 1) return controllers[0];

            for (int i = 0; i < controllers.Count; i++)
            {
                if (controllers[i].IsMainCockpit) return controllers[i];
            }

            return null;
        }

        //returns an Antenna, if there is one
        private IMyRadioAntenna getAntenna()
        {
            List<IMyRadioAntenna> antennas = new List<IMyRadioAntenna>();
            GridTerminalSystem.GetBlocksOfType<IMyRadioAntenna>(antennas);
            if (antennas.Count == 0) return null;
            return antennas[0];
        }


        /* * * * * * * * * * * *
         * Saves and Ini-Stuff *
         * * * * * * * * * * * */

        public void Save()
        {
            printIni();
        }

        private void loadIni()
        {

        }

        private void printIni()
        {

        }


        /* * * * * * * * * * *
         * Main and Commands *
         * * * * * * * * * * */

        public void Main(string argument, UpdateType updateSource)
        {
            /* When the game restarts it loads and executes the programmable block before it loads subgrids
             * this may lead to the script not finding blocks in subgrids in the constructor. Therefore, 
             * the initialization of blocklists is done at the next tick, when the pb updates with updateType.Once */
            //TODO: do this without the check in main. maybe a command?
            if ((updateSource & UpdateType.Once) != 0) init();

            //This ensures that the script will only run if it was either started manually or triggered itself
            if ((updateSource & (UpdateType.Update1 | UpdateType.Update10 | UpdateType.Update100)) != 0)
            {
                //only if someone is actually seated, otherwise nothing is done
                if (controller != null && controller.IsUnderControl)
                {
                    if (Runtime.UpdateFrequency != seated) Runtime.UpdateFrequency = seated;

                    float y = controller.MoveIndicator.Y;                                   //PlayerInput [ SPACE ] and [ C ]
                    float z = controller.MoveIndicator.Z;                                   //PlayerInput [ W ] and [ S ]
                    Vector3 forward = controller.WorldMatrix.Forward;                       //Normalized Vector forward relative to cockpit
                    Vector3 upward = controller.WorldMatrix.Up;                             //Normalized Vector upward relative to cockpit
                    Vector3 velocities = controller.GetShipVelocities().LinearVelocity;     //Momentum-Vector of the cockpit
                    float signedSpeed = forward.Dot(velocities);        //normal projection of the speed vector onto the forward vector to get a signed speed value

                    //handling brake lights
                    /* The y value changes to 1 or -1 on a keyboard.
                     * Unfortunatly, on a hosted singleplayer at least, the client- and the server-values are swapped.
                     * The Server-Player will cause this value to read 1, clients will cause it to read -1.
                     * It somehow still works with -1, but not all the time. Just checking if it changed at all
                     * causes the lights to light up when pressing [ C ] as well, but this "bug" is deliberatly
                     * accepted to guarantee functionality of the brake-lights when pressing [ SPACE ] */
                    if (brake != null)
                    {
                        if ((y != 0) || (signedSpeed > 1 && z > 0) || (signedSpeed < -1 && z < 0)) //braking
                        {
                            if (!brakesOn)
                            {
                                for (int i = 0; i < brake.Count; i++)
                                {
                                    brake[i].Color = brakeOnColor;
                                }
                                brakesOn = true;
                            }
                        }
                        else
                        {
                            if (brakesOn)
                            {
                                for (int i = 0; i < brake.Count; i++)
                                {
                                    brake[i].Color = brakeOffColor;
                                }
                                brakesOn = false;
                            }
                        }
                    }

                    //handling reverse lights
                    if (reverse != null)
                    {
                        if (reverseManual || signedSpeed < -0.1)   //reversing
                        {
                            if (!reverseOn)
                            {
                                for (int i = 0; i < reverse.Count; i++)
                                {
                                    reverse[i].Enabled = true;
                                }
                                reverseOn = true;
                            }
                        }
                        else
                        {
                            if (reverseOn)
                            {
                                for (int i = 0; i < reverse.Count; i++)
                                {
                                    reverse[i].Enabled = false;
                                }
                                reverseOn = false;
                            }
                        }
                    }

                    //handling spotlights
                    if (spotlights != null && !spotlightsOn)     //turning on the lights
                    {
                        for (int i = 0; i < spotlights.Count; i++)
                        {
                            spotlights[i].Enabled = true;
                        }
                        spotlightsOn = true;
                    }

                    /*Set strength of dampeners:
                     * 
                     * average the position of every wheel on its suspension when the car is not moving. add or remove strength until a desired value is reached
                     * 
                     * position of the wheel can not be accessed directly (i think) so we need to compare the offset on the vertical axis inbetween them
                     * 
                     * maybe even react to a moved center of mass
                     * */

                }
                else //if the cockpit is not under control..
                {
                    //..we are fine checking only every 100 ticks instead of every tick
                    if (Runtime.UpdateFrequency != UpdateFrequency.Update100) Runtime.UpdateFrequency = UpdateFrequency.Update100;
                    if (controller.HandBrake != true) controller.HandBrake = true;
                    if (spotlights != null && spotlightsOn)
                    {
                        for (int i = 0; i < spotlights.Count; i++)
                        {
                            spotlights[i].Enabled = false;
                        }
                        spotlightsOn = false;
                    }
                }
                //just to monitor performance
                Echo("LastRunTimeMS: " + Runtime.LastRunTimeMs.ToString("0.000000") +
                    "\nCurrent Instruction Count: " + Runtime.CurrentInstructionCount);
            }

            if ((updateSource & (UpdateType.Trigger | UpdateType.Terminal)) != 0)
            {
                //if a command is set
                switch (argument.ToLower())
                {
                    case "switch garage":
                        antenna.TransmitMessage("open garage " + Me.CubeGrid.CustomName.ToLower());
                        Me.CustomData += "\nopen garage " + Me.CubeGrid.CustomName.ToLower();
                        break;
                    case "switch reverse":
                        reverseManual = !reverseManual;
                        break;
                    case "unlock":
                        disconnect();
                        break;
                    case "lock":
                        connect();
                        break;
                }
            }
        }

        //connects the car to a close connector, pulls the hand brake, sets the batteries to recharge and saves the current ship weight to storage
        void connect()
        {

        }

        //disconnects the car from any connector, releases the hand brake, takes the batteries off recharge and sets the dampeners
        void disconnect()
        {
            float currentMass = controller.CalculateShipMass().PhysicalMass;
            float massRatio = currentMass / maxMass;
            //we only want to allow the connectors to unlock if the max mass is not surpassed
            if (massRatio <= 1)
            {
                for (int i = 0; i < wheels.Count; i++)
                {
                    wheels[i].setStrenght(massRatio);
                }
            }
        }
    }

    class Wheel
    {
        IMyMotorSuspension wheel;
        float maxStrength;
        String name;

        public Wheel(IMyMotorSuspension wheel, String name)
        {
            this.wheel = wheel;
            this.name = name;
        }

        //the massRatio is a value between 0 and 1, defining how many percent of the force shall be set
        public void setStrenght(float massRatio)
        {
            if (maxStrength != 0) wheel.Strength = maxStrength * massRatio;
        }

        //this cant be in the constructor, as it will only be known after loading the ini file
        //loading the ini file will be done separatly from loading the wheels as the values may change and be
        //reloaded, but not the wheels themselfs
        void setMaxStrength(float maxStrength)
        {
            this.maxStrength = maxStrength;
        }
    }
}