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
/*
ToDo:
Make antenna listen to only "elevator COMMAND ARGUMENTS" and commands themselfs still work without the leading "elevator"
take care of loading and saving issues
add testing prints
setup wheels automatically by comparing the forward vector with the way the wheels are facing
add thruster support


*/
        bool stopped;
        bool direction;
        Station next;
        Station marker;
        DateTime stopStamp = new DateTime();
        List<Station> stations = new List<Station>();
        String name = "Unnamed";
        int lastID = 0;
        String lastConfig = "";
        int tickCounter = 0;

        //blocks and values from the grid
        List<IMyMotorSuspension> wheels;
        List<IMyThrust> thrustersForward;
        List<IMyThrust> thrustersBackward;
        Vector3 forward;     //normalized upward/forward vector
        IMyRadioAntenna antenna;
        List<IMyTerminalBlock> screens;
        IMyShipController controller;

        Dictionary<string, Action> commands;
        MyCommandLine cline = new MyCommandLine();
        MyIni ini = new MyIni();

        //values to be manually set via custom data
        UpdateFrequency freq = UpdateFrequency.Update100;
        double maxSpeed = 50;    //UNIT?
        double brakeDist = 5;      //UNIT?




        /* * * * *
         * SETUP *
         * * * * */
        public Program()
        {
            //addings the commands to the dictionary
            commands = new Dictionary<string, Action>(StringComparer.OrdinalIgnoreCase);
            commands["request"] = request;
            commands["addstation"] = addStation;
            commands["removestation"] = removeStation;
            commands["setmarker"] = setMarker;

            //fetching blocks
            forward = Me.WorldMatrix.Forward;
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocks(blocks);
            getAntenna(ref blocks, out antenna);
            getTextPanels(ref blocks, out screens);
            getController(ref blocks, out controller);
            getWheels(ref blocks, out wheels);
            getThrusters(ref blocks, out thrustersForward, out thrustersBackward);
            name = antenna.HudText;

            Runtime.UpdateFrequency = freq;

            //if there is the controller missing we cannot do anything
            // PANIC I REPEAT PANIC
            if (controller == null)
            {
                Echo(" > PANIC <\n" +
                    "There is no controller. Please add a Remote Control or Cockpit to this grid and recompile.");
                Me.Enabled = false;
                return;
            }

            loadStorage();
            loadCustomData();
        }

        //returns an antenna if there are any
        private void getAntenna(ref List<IMyTerminalBlock> blocks, out IMyRadioAntenna antenna)
        {
            antenna = (IMyRadioAntenna)blocks.Find(x => x is IMyRadioAntenna);
        }

        //returns textpanels if there are any
        //the user can specify which ones to use by adding "elevator" to the custom name
        private void getTextPanels(ref List<IMyTerminalBlock> blocks, out List<IMyTerminalBlock> screens)
        {
            List<IMyTextPanel> panels = new List<IMyTextPanel>();
            if (blocks.Exists(x => x is IMyTextPanel && x.CustomName.ToLower().Contains("elevator")))
            {
                screens = blocks.FindAll(x => x is IMyTextPanel && x.CustomName.ToLower().Contains("elevator"));
            }
            else
            {
                screens = blocks.FindAll(x => x is IMyTextPanel);
            }
        }

        //returns a remote control (or cockpit, but there is none) if there are any
        //the user can specify which one to use by adding "elevator" to the custom name
        private void getController(ref List<IMyTerminalBlock> blocks, out IMyShipController controller)
        {
            if (blocks.Exists(x => x is IMyShipController && x.CustomName.ToLower().Contains("elevator")))
            {
                controller = (IMyShipController)blocks.Find(x => x is IMyShipController && x.CustomName.ToLower().Contains("elevator"));
            }
            else
            {
                controller = (IMyShipController)blocks.Find(x => x is IMyShipController);
            }
        }

        //returns a list of every wheel suspension that is enabled
        private void getWheels(ref List<IMyTerminalBlock> blocks, out List<IMyMotorSuspension> wheels)
        {
            List<IMyMotorSuspension> result = new List<IMyMotorSuspension>();
            foreach (IMyTerminalBlock block in blocks.FindAll(x => x is IMyMotorSuspension && x.IsWorking))
            {
                result.Add((IMyMotorSuspension)block);
            }
            wheels = result;
        }

        //returns a list of every wheel suspension that is enabled
        private void getThrusters(ref List<IMyTerminalBlock> blocks, out List<IMyThrust> thrustersForward, out List<IMyThrust> thrustersBackward)
        {
            List<IMyThrust> forwardTemp = new List<IMyThrust>();
            List<IMyThrust> backwardTemp = new List<IMyThrust>();


            foreach (IMyTerminalBlock block in blocks.FindAll(x => x is IMyThrust && x.IsWorking))
            {
                if (block.WorldMatrix.Up == forward) forwardTemp.Add((IMyThrust)block);                 //heavy testing required
                else if (block.WorldMatrix.Down == forward * -1) backwardTemp.Add((IMyThrust)block);    //heavy testing required
            }

            thrustersForward = forwardTemp;
            thrustersBackward = backwardTemp;
        }

        //we dont need this
        public void Save()
        {
			ini.Clear();
			String sec = "Save";
            ini.Set(sec, "lastID", lastID);
			ini.Set(sec, "lastConfig", lastConfig);
			ini.Set(sec, "tickCounter", tickCounter);
			ini.Set(sec, "name", name);
			ini.Set(sec, "UpdateFrequency", freq == UpdateFrequency.Update100 ? "100" : (freq == UpdateFrequency.Update10 ? "10" : "1"));
			ini.Set(sec, "maxSpeed", maxSpeed);
			ini.Set(sec, "brakeDist", brakeDist);
			String IDs = "";
			foreach (Station station in stations) {
				IDs += station.ID + " ";
			}
			ini.Set(sec, "IDs", IDs);
			
			foreach (Station station in stations) {
				sec = "Station " + station.ID;
				ini.Set(sec, "name", station.name);
				ini.Set(sec, "stopTime", station.stopTime);
				ini.Set(sec, "offset", station.offset);
				Vector3 position = station.position;
				ini.Set(sec, "Position", position.X + " " + position.Y + " " + position.Z);
			}
        }

        private void loadStorage()
        {
			MyIniParseResult result;
            if (Storage != "" && ini.TryParse(Storage, out result))
            {
				String freqString;
				String IDs;
                String sec = "Save";
				ini.Get(sec, "lastID").TryGetInt32(out lastID);
				ini.Get(sec, "lastConfig").TryGetString(out lastConfig);
				ini.Get(sec, "tickCounter").TryGetInt32(out tickCounter);
				ini.Get(sec, "name").TryGetString(out name);
				ini.Get(sec, "UpdateFrequency").TryGetString(out freqString);
				ini.Get(sec, "maxSpeed").TryGetDouble(out maxSpeed);
				ini.Get(sec, "brakeDist").TryGetDouble(out brakeDist);
				ini.Get(sec, "IDs").TryGetString(out IDs);
				
				freq = (freqString == "100" ? UpdateFrequency.Update100 : (freqString == "10" ? UpdateFrequency.Update10 : UpdateFrequency.Update1));
				String[] stationIDs = IDs.Split(' ');
				
				String stationName;
				int stationStopTime;
				int stationOffset;
				String positionString;
				Vector3 stationPosition;
				
				foreach (String ID in stationIDs) {
					sec = "Station " + ID;
					ini.Get(sec, "name").TryGetString(out stationName);
					ini.Get(sec, "stopTime").TryGetInt32(out stationStopTime);
					ini.Get(sec, "offset").TryGetInt32(out stationOffset);
					ini.Get(sec, "position").TryGetString(out positionString);
					String[] coords = positionString.Split(' ');
					stationPosition = new Vector3(float.Parse(coords[0]), float.Parse(coords[1]), float.Parse(coords[2]));
					
					
					Station temp = new Station(stationPosition, int.Parse(ID), stationName);
					temp.stopTime = stationStopTime;
					temp.offset = stationOffset;
					stations.Add(temp);
				}
				
				
				
                /* IMPORTANT:
                 * print current state to custom data
                 */
            }
        }

        private void loadCustomData()
        {
            String customData = Me.CustomData;
            if (customData != lastConfig && customData != "" && ini.TryParse(customData))
            {
                String sec = " General ";
                if (ini.ContainsSection(sec))
                {
                    if (ini.ContainsKey(new MyIniKey(sec, "name")))
                    {
                        if (!ini.Get(sec, "name").TryGetString(out name))
                        {
                            //do error message 
                        }
                    }
                    else
                    {
                        //do error message
                    }

                    if (ini.ContainsKey(new MyIniKey(sec, "UpdateFrequency")))
                    {
                        String freqString;
                        if (ini.Get(sec, "UpdateFrequency").TryGetString(out freqString))
                        {
                            //freq = (freqString == "100" ? UpdateFrequency.Update100 : (freqString == "10" ? UpdateFrequency.Update10 : UpdateFrequency.Update1));
                            if (freqString.Trim() == "100") freq = UpdateFrequency.Update100;
                            else if (freqString.Trim() == "10") freq = UpdateFrequency.Update10;
                            else if (freqString.Trim() == "1") freq = UpdateFrequency.Update1;
                            else
                            {
                                //do error message
                            }
                        }
                        else
                        {
                            //do error message
                        }

                    }

                    if (ini.ContainsKey(new MyIniKey(sec, "maxSpeed")))
                    {
                        if (!ini.Get(sec, "maxSpeed").TryGetDouble(out maxSpeed))
                        {
                            //do error message
                        }
                    }
                    else
                    {
                        //do error message
                    }

                    if (ini.ContainsKey(new MyIniKey(sec, "brakeDist")))
                    {
                        if (!ini.Get(sec, "brakeDist").TryGetDouble(out brakeDist))
                        {
                            //do error message
                        }
                    }
                    else
                    {
                        //do error message
                    }
                }
                else
                {
                    //do error message
                }

                foreach (Station station in stations)
                {
                    sec = "Station " + station.ID;
                    if (ini.ContainsSection(sec))
                    {
                        if (ini.ContainsKey(new MyIniKey(sec, "name")))
                        {
                            String temp;
                            if (ini.Get(sec, "name").TryGetString(out temp))
                            {
                                station.name = temp;
                            }
                            else
                            {
                                //do error message
                            }
                        }
                        else
                        {
                            //do error message
                        }

                        if (ini.ContainsKey(new MyIniKey(sec, "stopTime")))
                        {
                            int temp;
                            if (ini.Get(sec, "stopTime").TryGetInt32(out temp))
                            {
                                station.stopTime = temp;
                            }
                            else
                            {
                                //do error message
                            }
                        }
                        else
                        {
                            //do error message
                        }

                        if (ini.ContainsKey(new MyIniKey(sec, "offset")))
                        {
                            int temp;
                            if (ini.Get(sec, "offset").TryGetInt32(out temp))
                            {
                                station.offset = temp;
                            }
                            else
                            {
                                //do error message
                            }
                        }
                        else
                        {
                            //do error message
                        }
                    }
                    else
                    {
                        //do error message
                    }

                }
            }
			
			//if we came until here without an error, save to storage to save the changes
			Save();
        }

        private void saveCustomData()
        {
            //TODO: save current state to custom data
            ini.Clear();
			String sec = " General ";
			ini.Set(sec, "name", name);
			ini.Set(sec, "UpdateFrequency", freq == UpdateFrequency.Update100 ? "100" : (freq == UpdateFrequency.Update10 ? "10" : "1"));
			ini.Set(sec, "maxSpeed", maxSpeed);
			ini.Set(sec, "brakeDist", brakeDist);
			
			foreach (Station station in stations) {
				sec = "Station " + station.ID;
				ini.Set(sec, "name", station.name);
				ini.Set(sec, "stopTime", station.stopTime);
				ini.Set(sec, "offset", station.offset);
			}
        }



        /* * * * * * * * * *
         * Main & Commands *
         * * * * * * * * * */
        public void Main(string argument, UpdateType updateSource)
        {
            //if a command is communicated to the script
            if ((updateSource & (UpdateType.Antenna | UpdateType.Trigger | UpdateType.Terminal)) != 0)
            {
                //if arguments are actually given
                if (cline.TryParse(argument))
                {
                    Action cAction;

                    String command = cline.Argument(0).ToLower();

                    if (command == null)
                    {
                        Echo("No Command was given!");
                    }
                    else if (commands.TryGetValue(command, out cAction))
                    {
                        cAction();
                    }
                    else
                    {
                        Echo("Unknown Command!");
                    }
                }
            }

            //if the elevator triggered itself
            if ((updateSource & (UpdateType.Update100 | UpdateType.Update1)) != 0)
            {
                //raising the counter
                if ((updateSource & UpdateType.Update1) != 0) tickCounter++;
                else tickCounter += 100;

                //after a set time window, check custom data for updates
                if (tickCounter > 300)
                {
                    //TODO: check wether or not this actually triggers and if the %= actually works
                    loadCustomData();
                    tickCounter %= 300;
                }

                if (stopped)
                {
                    //converting the single number given in seconds into comparable ticks
                    if ((DateTime.Now.Ticks - stopStamp.Ticks) > next.stopTime * 10000000)
                    {
                        stopped = false;
                        next = null;
                    }
                }
                else  //if not stopped
                {
                    //if there is a station we want to drive to
                    if (next != null)
                    {
                        //check the distance to the station we want to drive to and set velocities accordingly
                        //TODO: IN WHAT UNIT!?
                        float vertDist = next.getVectorFromPosition(Me.GetPosition()).Dot(forward) + next.offset;
                        if (direction)
                        {
                            //if we reached our station
                            if (vertDist < 0)
                            {
                                stop();
                            }
                            else if (vertDist > brakeDist)
                            {
                                upwards(1, (float)maxSpeed);
                            }
                            else
                            {
                                float currentSpeed = getSpeed();
                                float wantedSpeed = Math.Max(vertDist * 10, 1);
                                if (currentSpeed < wantedSpeed) upwards(1, wantedSpeed);
                                else downwards(1, currentSpeed - wantedSpeed);
                            }
                        }
                        if (!direction)
                        {
                            if (vertDist > 0)
                            {
                                stop();
                            }
                            else if (vertDist < -brakeDist)
                            {
                                downwards(1, (float)maxSpeed);
                            }
                            else
                            {
                                //we need to calculate with negative numbers, which looks a bit odd at first
                                float currentSpeed = getSpeed();
                                float wantedSpeed = Math.Min(vertDist * 3, -1);
                                if (currentSpeed > wantedSpeed) downwards(1, wantedSpeed);
                                else upwards(1, wantedSpeed - currentSpeed);
                            }
                        }

                    }
                    else  //if there is no station we currently drive to
                    {
                        Station closestUp = null;
                        Station closestDown = null;
                        float distClosestUp = 0;
                        float distClosestDown = 0;
                        //check for requested stations
                        for (int i = 0; i < stations.Count; i++)
                        {
                            //oh, found one!
                            if (stations[i].requested)
                            {
                                float vertDist = stations[i].getVectorFromPosition(Me.GetPosition()).Dot(forward);
                                if (vertDist > 1)
                                {
                                    //only hold the closest ones, discard the rest
                                    if (closestUp == null)
                                    {
                                        closestUp = stations[i];
                                        distClosestUp = vertDist;
                                    }
                                    else
                                    {
                                        if (vertDist < distClosestUp)
                                        {
                                            closestUp = stations[i];
                                            distClosestUp = vertDist;
                                        }
                                    }
                                }
                                else if (vertDist < -1)
                                {
                                    //only hold the closest ones, discard the rest
                                    if (closestDown == null)
                                    {
                                        closestDown = stations[i];
                                        distClosestDown = vertDist;
                                    }
                                    else
                                    {
                                        if (vertDist > distClosestDown)
                                        {
                                            closestDown = stations[i];
                                            distClosestDown = vertDist;
                                        }
                                    }
                                }
                                //if a requested station is not more than 1m away it will be the station we are currently stopped in.
                                //the elevator should not move in this case, just open the door of the station
                                //maybe the player was afk and forgot to leave on time?
                                else
                                {
                                    antenna.TransmitMessage("open station " + stations[i].name);
                                    stations[i].requested = false;
                                }
                            }
                        }
                        if (direction)
                        {
                            //if there are further stations requested in the direction we are heading
                            if (closestUp != null)
                            {
                                next = closestUp;
                            }
                            //if there are no more stations in the direction we are currently heading
                            else if (closestDown != null)
                            {
                                next = closestDown;
                                direction = false;
                            }
                            //if there are no more stations requested at all
                            else
                            {
                                Runtime.UpdateFrequency = UpdateFrequency.None;
                            }
                        }
                        else
                        {
                            //if there are further stations requested in the direction we are heading
                            if (closestDown != null)
                            {
                                next = closestDown;
                            }
                            //if there are no more stations in the direction we are currently heading
                            else if (closestUp != null)
                            {
                                next = closestUp;
                                direction = true;
                            }
                            //if there are no more stations requested at all
                            else
                            {
                                Runtime.UpdateFrequency = UpdateFrequency.None;
                            }
                        }
                    }
                }
            }

            printScreens();

            //printing a little manual into the echo part
            Echo("    COMMANDS" +
                "\nrequest STATIONNAME: tells the machine to go to this station" +
                "\naddStation NAME X Y Z: adds the station with the given name and position to the elevator stations" +
                "\nremoveStation NAME: removes the station with the given name from the elevator stations");
        }

        //sets the request state of the station that fits the given name or id to true
        private void request()
        {
            //name or id
            String station = cline.Argument(1).ToLower();

            if (station == null)
            {
                Echo("No station name or ID was given!");
                return;
            }

            List<Station> requested = stations.FindAll(x => x.ID.ToString() == station || x.name.ToLower() == station);

            if (requested.Count > 1)
            {
                Echo("The given name or ID would fit several stations!");
                return;
            }
            if (requested.Count == 0)
            {
                Echo("No station was found with the given name or ID!");
                return;
            }

            requested[0].requested = true;
            if (isCloser(requested[0])) next = requested[0];
            Runtime.UpdateFrequency = freq;
        }

        private void addStation()
        {
            float x;
            float y;
            float z;
            String stationName = cline.Argument(1);

            for (int i = 0; i < stations.Count; i++)
            {
                if (stationName.ToLower() == stations[i].name.ToLower()) return;
            }

            float.TryParse(cline.Argument(2), out x);
            float.TryParse(cline.Argument(3), out y);
            float.TryParse(cline.Argument(4), out z);

            stations.Add(new Station(new Vector3(x, y, z), lastID, stationName));
            stations.Sort(); //TODO look into how to sort list by distance
        }

        private void removeStation()
        {
            String stationName = cline.Argument(1);
            if (stationName != null)
            {
                for (int i = 0; i < stations.Count; i++)
                {
                    if (stations[i].name == stationName)
                    {
                        if (stations.Remove(stations[i]))
                        {
                            Echo("Station was successfully removed!");
                            return;
                        }
                        Echo("There was an unexpected mistake when trying to remove the station.");
                        return;
                    }
                }
            }
        }

        //TODO: do we really need that?
        private void setMarker()
        {
            float x;
            float y;
            float z;
            float.TryParse(cline.Argument(1), out x);
            float.TryParse(cline.Argument(2), out y);
            float.TryParse(cline.Argument(3), out z);

            marker = new Station(new Vector3(x, y, z), -1);
        }

        private void printScreens()
        {

        }

        //returns wether or not the given Station is closer than the next station
        //TODO: check this, as this is sure as hell wrong
        private bool isCloser(Station toCheck)
        {
            if (next == null) return true;
            float distCheck = toCheck.getVectorFromPosition(Me.Position).Dot(forward);
            if (distCheck < 0 == direction) return false; //if the station to check is "behind" us we can break
            float distNext = next.getVectorFromPosition(Me.Position).Dot(forward);
            return (distNext > distCheck) == direction;
        }

        //returns a formatted string with all stations
        private String listStations()
        {
            if (stations.Count == 0) return "  There are no stations listed.";
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < stations.Count; i++)
            {
                //not adding stations that have "secret" in their name
                if (!stations[i].name.ToLower().Contains("secret"))
                {
                    result.Append("\n  " + stations[i].name +
                        "\n    isRequested: " + stations[i].requested);
                }
            }
            return result.ToString();
        }

        //returns the vertical velocity of the elevator wagon
        private float getSpeed()
        {
            Vector3 velocities = controller.GetShipVelocities().LinearVelocity;     //Momentum-Vector of the cockpit
            return forward.Dot(velocities);
        }



        /* * * * * * * * *
         * Wheel Control *
         * * * * * * * * */

        //setting everything for driving upwards
        private void upwards(float propulsionOverride, float speedLimit)
        {
            for (int i = 0; i < wheels.Count; i++)
            {
                wheels[i].SetValueFloat("Propulsion override", propulsionOverride);
                wheels[i].SetValueFloat("Speed Limit", speedLimit);

            }
            controller.HandBrake = false;
        }

        //setting everything for driving downwards
        private void downwards(float propulsionOverride, float SpeedLimit)
        {
            for (int i = 0; i < wheels.Count; i++)
            {
                wheels[i].SetValueFloat("Propulsion override", -propulsionOverride);
                wheels[i].SetValueFloat("Speed Limit", SpeedLimit);
            }
            controller.HandBrake = false;
        }

        //stopping
        private void stop()
        {
            for (int i = 0; i < wheels.Count; i++)
            {
                wheels[i].SetValueFloat("Propulsion override", 0);
            }
            controller.HandBrake = true;
            stopped = true;
            stopStamp = DateTime.Now;
            antenna.TransmitMessage("open station " + next.name);
            next.requested = false;
        }
    }

    class Station
    {
        public bool requested { set; get; }
        public String name { set; get; }
        public int ID { get; }   //-1 if marker
        public int stopTime { set; get; }
        public int offset { set; get; }
        public Vector3 position { get; }

        public Station(Vector3 position, int ID)
        {
            this.position = position;
            this.ID = ID;
            this.name = "unnamed";
        }

        public Station(Vector3 position, int ID, String name)
        {
            this.position = position;
            this.ID = ID;
            this.name = name;
        }

        //returns the vector between this station and the given position
        public Vector3 getVectorFromPosition(Vector3 point)
        {
            return (position - point);
        }
    }
}
