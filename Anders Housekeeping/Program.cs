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
         * calculate the time the refineries will be busy
         * 
         * spread ores over the refineries based on time needed until finished
         * 
         * sort chests
         * 
         * manage lights and shutters
         * 
         * airlocks
         * 
         * list percentage of fullness of chests and chestgroups on screens
         * 
         * craft a certain basis of components
         * 
         * clock screen
         * 
         * make script compatible with multiple elevators, supporting several doors and hangar doors
         * 
         * */

        Dictionary<String, Action> commands;
        MyCommandLine cline;

        //as the antenna can only broadcast one message at a time we need a queue
        Queue<String> broadcasts;
        IMyRadioAntenna antenna;
        bool sendable;

        // Depending on what an lcd panel has in its name a certain kind of content should be displayed
        //There can be as many screens displaying this content as the user wishes
        List<IMyTextPanel> logScreens;
        List<IMyTextPanel> chestScreens;
        List<IMyTextPanel> refineryScreens;
        List<IMyTextPanel> energyScreens;
        List<IMyCargoContainer> parts;
        List<IMyCargoContainer> ores;
        List<IMyRefinery> refineries;
        List<IMyAssembler> assemblers;
        String log;
        Queue<String> logQueue;


        //booleans to control what features are actually run
        bool useElevator;
        bool useGarages;

        List<Station> stations;
        List<GarageDoor> garages;

        /* * * * *
         * SETUP *
         * * * * */

        public Program()
        {
            //adding the commands to the dictionary
            commands = new Dictionary<string, Action>(StringComparer.OrdinalIgnoreCase);
            commands["open"] = open;
            cline = new MyCommandLine();

            //broadcasting stuff
            broadcasts = new Queue<string>();
            antenna = getAntenna();
            sendable = true;

            //filling the lists
            getTextpanels();
            getStations();
            getGarages();
            log = "";
            logQueue = new Queue<string>();

            useElevator = true;
            useGarages = true;
            Runtime.UpdateFrequency = UpdateFrequency.Update1;

            if (antenna != null) writeLog("Antenna found: " + antenna.CustomName);
            if (stations != null)
            {
                for (int i = 0; i < stations.Count; i++)
                {
                    writeLog("Station found: " + stations[i].name);
                }
            }
            if (garages != null)
            {
                for (int i = 0; i < garages.Count; i++)
                {
                    writeLog("garage found: " + garages[i].name);
                }
            }
            syncStations();
        }

        private void getStations()
        {
            //TODO write method that does not care about case when retrieving groups
            stations = new List<Station>();
            IMyBlockGroup temp = GridTerminalSystem.GetBlockGroupWithName("elevator doors");
            if (temp != null)
            {
                List<IMyDoor> doors = new List<IMyDoor>();
                temp.GetBlocksOfType(doors);
                for (int i = 0; i < doors.Count; i++)
                {
                    stations.Add(new Station(doors[i], doors[i].CustomName));
                }
            }
        }

        private void getGarages()
        {
            garages = new List<GarageDoor>();
            List<IMyBlockGroup> groups = new List<IMyBlockGroup>();
            GridTerminalSystem.GetBlockGroups(groups);

            for (int i = 0; i < groups.Count; i++)
            {
                //if we find a group that represents a garage
                if (groups[i].Name.ToLower().Contains("garage"))
                {
                    /* The groups that represent a garage should be named as follows:
                     * garage SHIPGRIDNAME
                     * Therefore we need to split the name of the group and use the second word
                     * in the groupname as the name for the garage
                     * */
                    String[] nameParts = groups[i].Name.Split(' ');
                    List<IMyAirtightHangarDoor> temp = new List<IMyAirtightHangarDoor>();
                    groups[i].GetBlocksOfType(temp);
                    if (temp.Count > 0)
                    {
                        garages.Add(new GarageDoor(nameParts[1], temp));
                    }
                }
            }
        }

        private void syncStations()
        {
            if (stations != null)
            {
                for (int i = 0; i < stations.Count; i++)
                {
                    Vector3 pos = stations[i].getPosition();
                    broadcasts.Enqueue("addStation " + stations[i].name + " " + pos.X + " " + pos.Y + " " + pos.Z);
                    writeLog("station sent: " + stations[i].name);
                }
            }
        }

        private IMyRadioAntenna getAntenna()
        {
            List<IMyRadioAntenna> antennas = new List<IMyRadioAntenna>();
            GridTerminalSystem.GetBlocksOfType<IMyRadioAntenna>(antennas);
            if (antennas.Count == 0) return null;
            return antennas[0];
        }

        private void getTextpanels()
        {
            //resetting the lists
            logScreens = new List<IMyTextPanel>();
            chestScreens = new List<IMyTextPanel>();
            refineryScreens = new List<IMyTextPanel>();
            energyScreens = new List<IMyTextPanel>();

            List<IMyTextPanel> panels = new List<IMyTextPanel>();
            GridTerminalSystem.GetBlocksOfType(panels);

            for (int i = 0; i < panels.Count; i++)
            {
                String name = panels[i].CustomName.ToLower();
                if (name.Contains("-log-")) logScreens.Add(panels[i]);
                if (name.Contains("-chest-")) chestScreens.Add(panels[i]);
                if (name.Contains("-refinery-")) refineryScreens.Add(panels[i]);
                if (name.Contains("-energy-")) energyScreens.Add(panels[i]);
            }
        }

        public void Save()
        {

        }


        /* * * * * * * * * * * * *
         * Writing to textpanels *
         * * * * * * * * * * * * */

        //adds the logline toWrite to the bottom of the screen, deletes the most top line if it would
        //exceed the height of the textpanel and adds the line to the custom data field, where
        //every log is saved until the script is recompiled
        private void writeLog(String toWrite)
        {
            if (logScreens.Count > 0)
            {
                const String header = "\n                                          LOG\n";
                String logText = "";
                logQueue.Enqueue(toWrite);
                if (logQueue.Count > 19) logQueue.Dequeue();
                String[] logLines = logQueue.ToArray();

                //adding all log lines into a single string
                for (int i = 0; i < logLines.Count(); i++)
                {
                    logText += "\n" + logLines[i];
                }

                //appending the logline to the custom data field and printing the log string
                for (int i = 0; i < logScreens.Count; i++)
                {
                    logScreens[i].CustomData += "\n" + toWrite;
                    logScreens[i].WritePublicText(header + logText);
                }
            }
        }

        private void drawChests()
        {

        }

        private void drawRefineries()
        {

        }

        private void drawEnergy()
        {

        }




        /* * * * * * * * * *
         * Main & Commands *
         * * * * * * * * * */

        public void Main(string argument, UpdateType updateSource)
        {
            //shit run every tick
            if ((updateSource & UpdateType.Update1) != 0)
            {
                //if we use the elevator
                if (useElevator && stations != null && stations.Count != 0)
                {
                    for (int i = 0; i < stations.Count; i++)
                    {
                        if (stations[i].Update())
                        {
                            broadcasts.Enqueue("request " + stations[i].name);
                        }
                    }
                }
            }

            //shit with arguments
            if ((updateSource & (UpdateType.Antenna | UpdateType.Trigger | UpdateType.Terminal)) != 0)
            {
                //if arguments are actually given
                if (cline.TryParse(argument))
                {
                    Action cAction;

                    String command = cline.Argument(0);

                    if (command == null)
                    {
                        writeLog("No Command was given!");
                    }
                    else if (commands.TryGetValue(command, out cAction))
                    {
                        cAction();
                    }
                    else
                    {
                        writeLog("Unknown Command: " + command);
                    }
                }
            }

            //at the end of every run we transmit one of the queued messages
            if (antenna != null && broadcasts.Count > 0)
            {
                if (sendable)
                {
                    antenna.TransmitMessage(broadcasts.Dequeue());
                    sendable = false;
                }
                else sendable = true;
            }
        }

        private void open()
        {
            String what = cline.Argument(1).ToLower();
            String which = cline.Argument(2).ToLower();

            if (what != null)
            {
                switch (what)
                {
                    case "station":
                        if (which != null)
                        {
                            for (int i = 0; i < stations.Count; i++)
                            {
                                if (stations[i].name.ToLower() == which)
                                {
                                    stations[i].open();
                                    return;
                                }
                            }
                        }
                        break;
                    case "garage":
                        if (useGarages == true && which != null)
                        {
                            for (int i = 0; i < garages.Count; i++)
                            {
                                if (garages[i].name.ToLower() == which)
                                {
                                    garages[i].toggleDoors();
                                    return;
                                }
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
        }
    }

    class Station
    {
        public String name { get; }
        IMyDoor door;
        DateTime opened;

        public Station(IMyDoor door, String name)
        {
            this.door = door;
            this.name = name;
            opened = new DateTime();
        }

        public Vector3 getPosition()
        {
            return door.GetPosition();
        }

        //returns wether the door was attempted to be opened or not.
        //in other words, if the elevator should go to this station or not
        public bool Update()
        {
            //if the door is being opened at the time since the last call of open() is more than 7 seconds ago
            //without the time check the door would automatically close everytime the station tries to open it.
            if (door.Status == DoorStatus.Opening && DateTime.Now.Ticks - opened.Ticks > 30000000)
            {
                door.CloseDoor();
                return true;
            }
            if (door.Status == DoorStatus.Open && DateTime.Now.Ticks - opened.Ticks > 30000000) door.CloseDoor();
            return false;
        }

        public void open()
        {
            door.OpenDoor();
            opened = DateTime.Now;
        }
    }

    class GarageDoor
    {
        public String name { get; }
        List<IMyAirtightHangarDoor> doors;
        bool open;

        public GarageDoor(String name, List<IMyAirtightHangarDoor> doors)
        {
            this.name = name;
            this.doors = doors;
            DoorStatus iniStatus = doors[0].Status;
            open = (iniStatus == DoorStatus.Open || iniStatus == DoorStatus.Opening ? true : false);
        }

        //if the doors are closed, open them
        //if the doors are open, close them
        public void toggleDoors()
        {
            if (open)
            {
                for (int i = 0; i < doors.Count; i++)
                {
                    doors[i].CloseDoor();
                }
            }
            else
            {
                for (int i = 0; i < doors.Count; i++)
                {
                    doors[i].OpenDoor();
                }
            }
            open = !open;
        }
    }
}