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
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {

        /*  Planning Notes:
         *  Production Management
         *      send content of chests via antenna
         *          eject everything beyond a certain quota of defined blocks
         *          stack items in chest
         *      take care of filling of refineries
         *              -> fill refineries depending on speed/yield modules
         *              -> set preference for speed or yield modules
         *              -> display speed or yield seperatly with remaining time
         *              -> yield may help speed module refineries until yield materials are back, not the other way around, displayed as (helping)
         *      ejector management
         *          -> if more than quota of component, ingot or ore is present, eject it via predefined connector.
         *  Clock / Lights
         *  
         *  Elevator
		 *		Master:
		 *	        knows of lines
		 *	        setup of lines via checklist
		 *		        when first is selected the list of possible stations is narrowed down in all directions with a certain threshhold (two blocks?)
		 *		        when second is selected the list of possible stations is narrowed down further in the selected direction
		 *	        allows setting of threshhold in large blocks as unit
		 *	
		 *	        line:
		 *		        stations
		 *			        list of doors, button panels
         *			        
		 *		        recharge station
		 *         
		 *       Wagon:
         *  
         *  Garage Door Management
         *  
         *  Solar Panel Adjustment
         *  
         *  airlocks
         *  
         *  Energy Management / Power Display
         *      turn on engines if output > current max production
         *      display some values
         *  Master:
         *      make all screens accessible via the output screen, so separate screens are not a necessity
         * */

        //controlling what is activated
        bool enableAirlocks = false;
        bool enableClock = false;
        bool enableElevator = false;
        bool enableEnergyManagement = false;
        bool enableGarageManagement = false;
        bool enableProductionManagement = false;
        bool enableSolarpanelAdjustment = false;

        //vitality check booleans
        bool canMaster = false;
        bool canAirlocks = false;
        bool canClock = false;
        bool canElevator = false;
        bool canEnergyManagement = false;
        bool canGarageManagement = false;
        bool canProductionManagement = false;
        bool canSolarpanelAdjustment = false;

        //Functionality Objects
        Airlocks airlocks;
        Clock clock;
        Elevator elevator;
        EnergyManagement energy;
        GarageManagement garage;
        ProductionManagement production;
        SolarAdjustment solar;


        //other framework variables
        MyCommandLine commandline = new MyCommandLine();
        Dictionary<string, Action> commands = new Dictionary<string, Action>(StringComparer.OrdinalIgnoreCase);
        MyIni ini = new MyIni();
        IMyProgrammableBlock me;
        Menu main;
        Menu current;
        List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
        Queue<string> logQueue = new Queue<string>();
        IMyTextPanel output;
        IMyTextPanel logs;
        IMyTextPanel debug;
        bool fetchedBlocks = false;
        int updateCount = 0;


        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            GridTerminalSystem.GetBlocks(blocks);
            fetchedBlocks = true;
            me = Me;

            //check for master screen
            IMyTerminalBlock temp = blocks.Find(x => x.CustomName.Contains("[Master]"));
            if (temp is IMyTextPanel) output = (IMyTextPanel)temp;
            if (output != null) writeLog("Master output screen found");
            else writeLog("No Master output screen found! Please add the tag [Master] to the name of a text panel and recompile this script.");

            main = new Menu(null, "    Main Menu", "Main Menu", this);
            current = main;

            String customData = Me.CustomData;

            if (Storage != "" && ini.TryParse(Storage))
            {
                writeLog("Parseable Ini found in Storage");
                loadIni(true);
            }
            else
            {
                writeLog("Storage is empty or unreadable");
                if (customData != "" && ini.TryParse(customData))
                {
                    writeLog("Parseable Ini found in Custom Data");
                    loadIni();
                }
                else
                {
                    writeLog("Custom Data is empty or unreadable");
                }
            }

            createMenu();

            if (output != null) reprintMenu();

            //commands that the script needs to understand
            commands["up"] = up;
            commands["down"] = down;
            commands["enter"] = enter;
            commands["back"] = back;
        }

        //Copying the ini to the Storage String
        public void Save()
        {
            printIni(true);
        }

        public void Main(string argument, UpdateType updateSource)
        {
            try
            {
                /*  can booleans, false by default
                 *  
                 *  if runnable -> do shit
                 *  if not runnable -> do nothing, check everything if runnable again.
                 *      if yes, set runnable to true
                 *      if no put out some information about what is missing.
                 * 
                 * 
                 * */

                fetchedBlocks = false;

                //command handling
                if ((updateSource & (UpdateType.Antenna | UpdateType.Terminal | UpdateType.Trigger)) != 0)
                {
                    if (commandline.TryParse(argument))
                    {
                        Action commandAction;
                        String command = commandline.Argument(0);
                        if (command != null)
                        {
                            if (commands.TryGetValue(command, out commandAction))
                            {
                                commandAction();
                            }
                            else
                            {
                                writeLog("Command was not recognized, dumbfuck");
                            }
                        }
                    }
                }
                else
                {
                    //if we increase this at the end of the main, pressing buttons fucks up the timings
                    updateCount++;
                }

                //doing regular stuff
                if ((updateSource & UpdateType.Update10) != 0)
                {
                    if (canMaster)
                    {
                        //do regular stuff
                    }
                    else
                    {
                        //do vitality check every 5 seconds.
                        // if runnable -> set runnable to true
                        // if not runnable -> echo some information about what is missing
                    }

                    if (enableProductionManagement && updateCount % 6 == 0)
                    {
                        if (canProductionManagement)
                        {
                            if (updateCount % 180 == 0) production.fetchBlocks();
                            production.run();
                        }
                        else
                        {

                        }
                    }

                    if (enableEnergyManagement && updateCount % 30 == 0)
                    {
                        if (canEnergyManagement)
                        {

                        }
                    }

                }
                if (debug != null)
                {
                    /*
                        bool enableAirlocks = false;
                        bool enableClock = false;
                        bool enableElevator = false;
                        bool enableEnergyManagement = false;
                        bool enableGarageManagement = false;
                        bool enableProductionManagement = false;
                        bool enableSolarpanelAdjustment = false; 
                     * */
                    String debugtext = "\n                                        DEBUG\n" +
                        "\nuse / can Master:                true | " + canMaster +
                        "\nuse / can Airlocks:              " + enableAirlocks + " | " + canAirlocks +
                        "\nuse / can Clock:                 " + enableClock + " | " + canClock +
                        "\nuse / can Elevator:              " + enableElevator + " | " + canElevator +
                        "\nuse / can Energy Management:     " + enableEnergyManagement + " | " + canEnergyManagement +
                        "\nuse / can Garage Management:     " + enableGarageManagement + " | " + canGarageManagement +
                        "\nuse / can Production Management: " + enableProductionManagement + " | " + canProductionManagement +
                        "\nuse / can Solarpanel Adjustment: " + enableSolarpanelAdjustment + " | " + canSolarpanelAdjustment +
                        "\nInstruciton count: " + Runtime.CurrentInstructionCount + "/" + Runtime.MaxInstructionCount +
                        "\nLast Total Runtime: " + Runtime.LastRunTimeMs.ToString("0.000000") +
                        "\nLast PM analyze Runtime: " + production.runtimeAnalyze +
                        "\nLast PM calculate Runtime: " + production.runtimeCalculate +
                        "\nLast PM fill ass Runtime: " + production.runtimeFillAssemblers +
                        "\nLast PM fill ref Runtime: " + production.runtimeFillRefineries;
                    debug.WritePublicText(debugtext);
                }


                writeLog(null, true);

            }
            catch (Exception e)
            {
                writeLog("* * * * * * * * * * *\n" +
                    "* Exception caught! *\n" +
                    "* * * * * * * * * *\n" +
                    "Please contact the author of this script with the following text and a detailed description of what you did." +
                    "\nType: " + e.GetType() +
                    "\nSource: " + e.Source +
                    "\nMessage: " + e.Message +
                    "\nStacktrace:\n" + e.StackTrace, true);
            }
        }

        //menu drawing


        void createMenu()
        {
            //Master
            Menu Master = new Menu(main, "    Main Menu > Master", "Master", this);
            Master.addChild(new MenuAction(Master, "Load Config", loadIni, this));
            Master.addChild(new MenuAction(Master, "Print Config", printIni, this));
            //Menu selectLogScreen = new Menu(Master, "    Main Menu > Master > Log-Screen Selection", "Select Log-Screen", this);
            Master.addChild(new MenuAction(Master, "Select Log-Screen", Master.addBlocksAsRadio, this));
            Master.addChild(new MenuAction(Master, "Select Debug-Screen", Master.addBlocksAsRadio, this));
            Master.addChild(new MenuAction(Master, "Select Functionalities to use", Master.addFunctionalities, this));
            main.addChild(Master);

            //make visibility of the menu thingies below here dependent on them being enabled or not

            //Airlocks

            //Clock

            //Elevator

            //Energy Management
            //Menu EneManagement = new Menu(main, "    Main Menu > Energy Management", "Energy Management", this);
            //EneManagement.setLabel("Here you will be able to configure the Energy Management.");
            //main.addChild(EneManagement);

            //Garage Management

            //Production Management
            Menu ProManagement = new Menu(main, "    Main Menu > Production Management", "Production Management", this);
            ProManagement.addChild(new MenuAction(ProManagement, "Select Ore Quota Screens", ProManagement.addBlocksAsCheck, this));
            ProManagement.addChild(new MenuAction(ProManagement, "Select Ingot Quota Screens", ProManagement.addBlocksAsCheck, this));
            ProManagement.addChild(new MenuAction(ProManagement, "Select Component1 Quota Screens", ProManagement.addBlocksAsCheck, this));
            ProManagement.addChild(new MenuAction(ProManagement, "Select Component2 Quota Screens", ProManagement.addBlocksAsCheck, this));
            ProManagement.addChild(new MenuAction(ProManagement, "Select Tools Quota Screens", ProManagement.addBlocksAsCheck, this));
            ProManagement.addChild(new MenuAction(ProManagement, "Select Industry Status Screens", ProManagement.addBlocksAsCheck, this));
            main.addChild(ProManagement);

            //Solarpanel Adjustment
        }

        void reprintMenu()
        {
            if (output != null) output.WritePublicText(current.toString());
            else writeLog("No Master screen defined!");
        }


        //menu. the command dictionary cannot take the current.method() directly, as it would link to the instance, not the variable
        void up()
        {
            current.up();
        }

        void down()
        {
            current.down();
        }

        void enter()
        {
            current.enter();
        }

        void back()
        {
            current.back();
        }


        //ini stuff
        void loadIni()
        {
            loadIni(false);
        }

        void loadIni(bool fromStorage)
        {
            writeLog("Attempting to load from " + (fromStorage ? "Storage" : "Custom Data"));
            //parsing the ini
            MyIniParseResult result;
            ini.Clear();
            String toParse = (fromStorage ? Storage : Me.CustomData);
            if (toParse != "")
            {
                if (!ini.TryParse(toParse, out result))
                {
                    writeLog("Failed at reading config from " + (fromStorage ? "Storage:" : "CustomData:"));
                    writeLog(result.ToString());
                    writeLog("To print empty config select \"print config\" in the main menu");
                    canMaster = false;
                    return;
                }
                writeLog("Successfully parsed config from " + (fromStorage ? "Storage" : "Custom Data"));

                //reading the parsed ini

                /* * * * * * * * * *
                 * Master Settings *
                 * * * * * * * * * */
                String section = " Master ";
                if (ini.ContainsSection(section))
                {
                    if (ini.ContainsKey(new MyIniKey(section, "Log-Screen")))
                    {
                        long logsID;
                        if (ini.Get(section, "Log-Screen").TryGetInt64(out logsID))
                        {
                            IMyTerminalBlock temp = idToBlock(logsID);
                            if (temp != null && output != temp) logs = (IMyTextPanel)temp;
                            else writeLog("No screen found with given Log-Screen Entity-ID");
                        }
                    }
                    else
                    {
                        writeLog("No Log-Screen found in config");
                        canMaster = false;
                    }

                    if (ini.ContainsKey(new MyIniKey(section, "Debug-Screen")))
                    {
                        long logsID;
                        if (ini.Get(section, "Debug-Screen").TryGetInt64(out logsID))
                        {
                            IMyTerminalBlock temp = idToBlock(logsID);
                            if (temp != null) debug = (IMyTextPanel)temp;
                            else writeLog("No screen found with given Log-Screen Entity-ID");
                        }
                    }
                    else
                    {
                        writeLog("No Debug-Screen found in config");
                        canMaster = false;
                    }
                }
                else
                {
                    writeLog("No Master section found in config");
                    canMaster = false;
                }

                /* * * * * * * * * *
                 * Functionalities *
                 * * * * * * * * * */
                section = " Functionalities ";
                if (ini.ContainsSection(section))
                {
                    loadFunctionality(ref ini, "use Airlocks", out enableAirlocks);
                    loadFunctionality(ref ini, "use Clock", out enableClock);
                    loadFunctionality(ref ini, "use Elevator", out enableElevator);
                    loadFunctionality(ref ini, "use Energy Management", out enableEnergyManagement);
                    loadFunctionality(ref ini, "use Garage Management", out enableGarageManagement);
                    loadFunctionality(ref ini, "use Production Management", out enableProductionManagement);
                    loadFunctionality(ref ini, "use Solarpanel Adjustment", out enableSolarpanelAdjustment);
                }
                else
                {
                    writeLog("No Functionalities section found in config");
                    canMaster = false;
                }

                /* * * * * *
                 * Airlock *
                 * * * * * */
                if (enableAirlocks)
                {
                    airlocks = new Airlocks(this);
                    canAirlocks = true;
                    section = " Airlocks ";
                }

                /* * * * *
                 * Clock *
                 * * * * */
                if (enableClock)
                {
                    clock = new Clock(this);
                    canClock = true;
                    section = " Clock ";
                }

                /* * * * * * *
                 * Elevators *
                 * * * * * * */
                if (enableElevator)
                {
                    elevator = new Elevator(this);
                    canElevator = true;
                    section = " Elevator ";
                }

                /* * * * * * * * * * *
                 * Energy Management *
                 * * * * * * * * * * */
                if (enableEnergyManagement)
                {
                    energy = new EnergyManagement(this);
                    canEnergyManagement = true;
                    section = " Energy Management ";
                }

                /* * * * * * * * * * *
                 * Garage Management *
                 * * * * * * * * * * */
                if (enableGarageManagement)
                {
                    garage = new GarageManagement(this);
                    canGarageManagement = true;
                    section = " Garage Management ";
                }

                /* * * * * * * * * * * * *
                 * Production Management *
                 * * * * * * * * * * * * */
                if (enableProductionManagement)
                {
                    production = new ProductionManagement(this);
                    canProductionManagement = true;
                    section = " Production Management ";
                    if (ini.ContainsSection(section))
                    {
                        /*
                        public List<IMyTextPanel> oresScreen;
                        public List<IMyTextPanel> ingotsScreen;
                        public List<IMyTextPanel> components1Screen;
                        public List<IMyTextPanel> components2Screen;
                        public List<IMyTextPanel> toolsScreen;
                        */

                        //ores-quota-screen
                        if (ini.ContainsKey(new MyIniKey(section, "Ores-Quota-Screen")))
                        {
                            String multiline;
                            String[] lines;
                            ini.Get(section, "Ores-Quota-Screen").TryGetString(out multiline);
                            if (multiline != "")
                            {
                                lines = multiline.Split('\n');
                                foreach (String line in lines)
                                {
                                    long screenID;
                                    if (long.TryParse(line, out screenID))
                                    {
                                        IMyTerminalBlock temp = idToBlock(screenID);
                                        production.oresScreen.Add((IMyTextPanel)temp);
                                    }
                                    else writeLog("Ores-Quota-Screen line could not be parsed to long: " + line);
                                }
                                writeLog("");
                            }
                        }
                        else writeLog("No Ores-Quota-Screen found in config");

                        //ingots-quota-screen
                        if (ini.ContainsKey(new MyIniKey(section, "Ingots-Quota-Screen")))
                        {
                            String multiline;
                            String[] lines;
                            ini.Get(section, "Ingots-Quota-Screen").TryGetString(out multiline);
                            if (multiline != "")
                            {
                                lines = multiline.Split('\n');
                                foreach (String line in lines)
                                {
                                    long screenID;
                                    if (long.TryParse(line, out screenID))
                                    {
                                        IMyTerminalBlock temp = idToBlock(screenID);
                                        production.ingotsScreen.Add((IMyTextPanel)temp);
                                    }
                                    else writeLog("Ingots-Quota-Screen line could not be parsed to long: " + line);
                                }
                            }
                        }
                        else writeLog("No Ingots-Quota-Screen found in config");

                        //components1-quota-screen
                        if (ini.ContainsKey(new MyIniKey(section, "Components1-Quota-Screen")))
                        {
                            String multiline;
                            String[] lines;
                            ini.Get(section, "Components1-Quota-Screen").TryGetString(out multiline);
                            if (multiline != "")
                            {
                                lines = multiline.Split('\n');
                                foreach (String line in lines)
                                {
                                    long screenID;
                                    if (long.TryParse(line, out screenID))
                                    {
                                        IMyTerminalBlock temp = idToBlock(screenID);
                                        production.components1Screen.Add((IMyTextPanel)temp);
                                    }
                                    else writeLog("Components1-Quota-Screen line could not be parsed to long: " + line);
                                }
                            }
                        }
                        else writeLog("No Components1-Quota-Screen found in config");

                        //components2-quota-screen
                        if (ini.ContainsKey(new MyIniKey(section, "Components2-Quota-Screen")))
                        {
                            String multiline;
                            String[] lines;
                            ini.Get(section, "Components2-Quota-Screen").TryGetString(out multiline);
                            if (multiline != "")
                            {
                                lines = multiline.Split('\n');
                                foreach (String line in lines)
                                {
                                    long screenID;
                                    if (long.TryParse(line, out screenID))
                                    {
                                        IMyTerminalBlock temp = idToBlock(screenID);
                                        production.components2Screen.Add((IMyTextPanel)temp);
                                    }
                                    else writeLog("Components2-Quota-Screen line could not be parsed to long: " + line);
                                }
                            }
                        }
                        else writeLog("No Components2-Quota-Screen found in config");

                        //tools-quota-screen
                        if (ini.ContainsKey(new MyIniKey(section, "Tools-Quota-Screen")))
                        {
                            String multiline;
                            String[] lines;
                            ini.Get(section, "Tools-Quota-Screen").TryGetString(out multiline);
                            if (multiline != "")
                            {
                                lines = multiline.Split('\n');
                                foreach (String line in lines)
                                {
                                    long screenID;
                                    if (long.TryParse(line, out screenID))
                                    {
                                        IMyTerminalBlock temp = idToBlock(screenID);
                                        production.toolsScreen.Add((IMyTextPanel)temp);
                                    }
                                    else writeLog("Tools-Quota-Screen line could not be parsed to long: " + line);
                                }
                            }
                        }
                        else writeLog("No Tools-Quota-Screen found in config");

                        //industry-status-screen
                        if (ini.ContainsKey(new MyIniKey(section, "Industry-Status-Screen")))
                        {
                            String multiline;
                            String[] lines;
                            ini.Get(section, "Industry-Status-Screen").TryGetString(out multiline);
                            if (multiline != "")
                            {
                                lines = multiline.Split('\n');
                                foreach (String line in lines)
                                {
                                    long screenID;
                                    if (long.TryParse(line, out screenID))
                                    {
                                        IMyTerminalBlock temp = idToBlock(screenID);
                                        production.industryScreen.Add((IMyTextPanel)temp);
                                    }
                                    else writeLog("Industry-Status-Screen line could not be parsed to long: " + line);
                                }
                            }
                        }
                        else writeLog("No Industry-Status-Screen found in config");
                    }
                    else
                    {
                        writeLog("No Production Management section found in config");
                        canProductionManagement = false;
                    }
                }

                /* * * * * * * * * * * * *
                 * Solarpanel Adjustment *
                 * * * * * * * * * * * * */
                if (enableSolarpanelAdjustment)
                {
                    solar = new SolarAdjustment(this);
                    canSolarpanelAdjustment = true;
                    section = " Solarpanel Adjustment ";
                }
            }
            else
            {
                writeLog((fromStorage ? "Storage" : "CustomData") + "is empty, loading defaults");
            }

            //printing the current state
            //this is necessary as the ini that needs to be printed changes based on the settings set
            printIni();

        }


        void loadFunctionality(ref MyIni ini, String key, out bool enabler)
        {
            String section = " Functionalities ";
            if (ini.ContainsKey(new MyIniKey(section, key)))
            {
                if (!ini.Get(section, key).TryGetBoolean(out enabler))
                {
                    writeLog(key + " requires either true, 1, false or 0");
                }
            }
            else
            {
                writeLog("No " + key + " found in config");
                enabler = false;
                canMaster = false;
            }
        }


        void printIni()
        {
            printIni(false);
        }

        void printIni(bool toStorage)
        {
            ini.Clear();

            String section = " Master ";
            ini.Set(section, "Log-Screen", (logs == null ? "" : "" + logs.EntityId));
            ini.Set(section, "Debug-Screen", (debug == null ? "" : "" + debug.EntityId));
            ini.SetSectionComment(section, " Here you can configure the master script. To apply any changes you made" +
                "\n here you need to select \"load\" in the config menu. This configuration" +
                "\n text will be reprinted whenever there are changes that influence values" +
                "\n you see here.");

            section = " Functionalities ";
            ini.Set(section, "use Airlocks", enableAirlocks);
            ini.Set(section, "use Clock", enableClock);
            ini.Set(section, "use Elevator", enableElevator);
            ini.Set(section, "use Energy Management", enableEnergyManagement);
            ini.Set(section, "use Garage Management", enableGarageManagement);
            ini.Set(section, "use Production Management", enableProductionManagement);
            ini.Set(section, "use Solarpanel Adjustment", enableSolarpanelAdjustment);
            ini.SetSectionComment(section, " Here you can decide which functionalities to use. Once you are done setting" +
                "\n these values you need to select \"load\" in the config menu so that the" +
                "\n respective configuration sections will be printed.");


            //checking for enabled functionalities
            //only if they are enabled, their configuration options are printed to avoid synchronization traffic as much as possible and to keep the ini simple
            if (enableAirlocks)
            {
                section = " Airlocks ";
                ini.Set(section, "Dummy Key", "Dummy Value");
            }
            if (enableClock)
            {
                section = " Clock ";
                ini.Set(section, "Dummy Key", "Dummy Value");
            }
            if (enableElevator)
            {
                section = " Elevator ";
                ini.Set(section, "Dummy Key", "Dummy Value");
            }
            if (enableEnergyManagement)
            {
                section = " Energy Management ";
                ini.Set(section, "Dummy Key", "Dummy Value");
            }
            if (enableGarageManagement)
            {
                section = " Garage Management ";
                ini.Set(section, "Dummy Key", "Dummy Value");
            }
            if (enableProductionManagement)
            {
                section = " Production Management ";
                ini.Set(section, "Ores-Quota-Screen", listToMultiline(production.oresScreen));
                ini.Set(section, "Ingots-Quota-Screen", listToMultiline(production.ingotsScreen));
                ini.Set(section, "Components1-Quota-Screen", listToMultiline(production.components1Screen));
                ini.Set(section, "Components2-Quota-Screen", listToMultiline(production.components2Screen));
                ini.Set(section, "Tools-Quota-Screen", listToMultiline(production.toolsScreen));
                ini.Set(section, "Industry-Status-Screen", listToMultiline(production.industryScreen));
            }
            if (enableSolarpanelAdjustment)
            {
                section = " Solarpanel Adjustment  ";
                ini.Set(section, "Dummy Key", "Dummy Value");
            }

            if (toStorage) Storage = ini.ToString();
            else Me.CustomData = ini.ToString();
        }

        String listToMultiline(List<IMyTerminalBlock> blocks)
        {
            String result = "";
            foreach (IMyTerminalBlock block in blocks)
            {
                result += block.EntityId + "\n";
            }
            return result;
        }

        String listToMultiline(List<IMyTextPanel> blocks)
        {
            String result = "";
            foreach (IMyTextPanel block in blocks)
            {
                result += block.EntityId + "\n";
            }
            return result;
        }




        //utility
        IMyTerminalBlock idToBlock(long id)
        {
            fetchBlocks();
            return blocks.Find(x => x.EntityId == id);
        }

        void fetchBlocks()
        {
            if (!fetchedBlocks) GridTerminalSystem.GetBlocks(blocks);
        }

        bool usesScreen(IMyTerminalBlock panel)
        {
            return panel == output || panel == debug || panel == logs;
        }

        String loadingBar(double ratio)
        {
            String result = "[";
            int granularity = 10;       //maybe make this a parameter?
            for (int i = 1; i <= ratio * granularity && i <= granularity; i++)
            {
                result += "I";
            }
            for (int i = result.Length - 1; i < granularity; i++)
            {
                result += "´";
            }
            return result + "]";
        }

        bool parseNumber(String number, out int result)
        {
            number = number.Replace(" ", "");
            String toParse;
            int factor;
            switch (number.Last())
            {
                case 'm':
                    factor = 1000000;
                    toParse = number.Substring(0, number.Length - 1);
                    break;
                case 'k':
                    factor = 1000;
                    toParse = number.Substring(0, number.Length - 1);
                    break;
                default:
                    factor = 1;
                    toParse = number;
                    break;
            }

            if (int.TryParse(toParse, out result))
            {
                result *= factor;
                return true;
            }
            writeLog("Could not parse number >" + toParse + "<");
            result = 0;
            return false;
        }

        String formatNumber(int number)
        {
            if (number < 1000) return number + "";
            else if (number < 1000000) return number / 1000 + "k";
            else return number / 1000000 + "m";
        }


        //health
        bool validityCheck()
        {
            //check for report screen
            return true;
        }

        bool isRunnable()
        {
            return canAirlocks && canClock && canElevator && canEnergyManagement && canGarageManagement && canProductionManagement && canSolarpanelAdjustment;
        }


        //adds the logline toWrite to the bottom of the screen, deletes the most top line if it would
        //exceed the height of the textpanel and adds the line to the custom data field, where
        //every log is saved until the script is recompiled
        public void writeLog(String toWrite, bool writeEcho = false)
        {
            if (toWrite != null)
            {
                logQueue.Enqueue(toWrite);
                if (logQueue.Count > 19) logQueue.Dequeue();
            }

            //if we need to write the log instead of just adding stuff for a later echo output
            if (logs != null || writeEcho)
            {
                const String header = "\n                                          LOG\n";
                String logText = "";
                String[] logLines = logQueue.ToArray();

                //adding all log lines into a single string
                for (int i = 0; i < logLines.Length; i++)
                {
                    logText += "\n" + logLines[i];
                }

                if (logs != null) logs.WritePublicText(header + logText);
                if (writeEcho) Echo(logText);
            }
        }


        /* * * * *
         * Menus *
         * * * * */

        class MenuObject
        {
            public String name;
            public Program pro;
            public Menu parent;
            public long entityId;

            //for subclasses
            public MenuObject(String name, Program pro, Menu parent)
            {
                this.name = name;
                this.pro = pro;
                this.parent = parent;
            }

            //for block representation in menus
            public MenuObject(String name, long entityId, Menu parent)
            {
                this.name = name;
                this.entityId = entityId;
                this.parent = parent;
            }
        }

        class Menu : MenuObject
        {
            protected String header;
            protected String label;
            protected int index = 0;
            protected int page = 0;
            protected List<MenuObject> children;

            //constructors
            public Menu(Menu parent, String header, String name, Program pro) : base(name, pro, parent)
            {
                children = new List<MenuObject>();
                this.header = header;
            }

            //adders & setters
            public void addChild(MenuObject value)
            {
                if (!hasChild(value.name)) children.Add(value);

            }

            public bool hasChild(String name)
            {
                foreach (MenuObject temp in children)
                {
                    if (temp.name == name) return true;
                }
                return false;
            }

            public void removeChild(String name)
            {
                foreach (MenuObject temp in children)
                {
                    if (temp.name == name)
                    {
                        children.Remove(temp);
                        return;
                    }
                }
            }

            public void setLabel(string label) { this.label = label; }

            public void setIndex(int index) { this.index = index; }

            //Action methods required for MenuActions
            public void addFunctionalities()
            {
                CheckMenu checkmenu = new CheckMenu(this, "    Select Functionalities to use", "Select Functionalities", pro);
                checkmenu.setLabel("Choose which functionalities to use");
                //checkmenu.addChild(new MenuObject("Airlocks", null, checkmenu), pro.enableAirlocks);
                //checkmenu.addChild(new MenuObject("Clocks", null, checkmenu), pro.enableClock);
                //checkmenu.addChild(new MenuObject("Elevator", null, checkmenu), pro.enableElevator);
                checkmenu.addChild(new MenuObject("Energy Management", null, checkmenu), pro.enableEnergyManagement);
                //checkmenu.addChild(new MenuObject("Garage Management", null, checkmenu), pro.enableGarageManagement);
                checkmenu.addChild(new MenuObject("Production Management", null, checkmenu), pro.enableProductionManagement);
                //checkmenu.addChild(new MenuObject("Solarpanel Adjustment", null, checkmenu), pro.enableSolarpanelAdjustment);

                pro.current = checkmenu;
            }

            public void addBlocksAsRadio()
            {
                switch (children[index].name)
                {
                    case "Select Log-Screen":
                        pro.fetchBlocks();
                        List<IMyTerminalBlock> screensLogs = pro.blocks.FindAll(x =>
                                x is IMyTextPanel
                                && x != pro.output
                                && x != pro.debug
                                && (pro.airlocks == null || !pro.airlocks.usesScreen(x))
                                && (pro.clock == null || !pro.clock.usesScreen(x))
                                && (pro.elevator == null || !pro.elevator.usesScreen(x))
                                && (pro.energy == null || !pro.energy.usesScreen(x))
                                && (pro.garage == null || !pro.garage.usesScreen(x))
                                && (pro.production == null || !pro.production.usesScreen(x))
                                && (pro.solar == null || !pro.solar.usesScreen(x)));
                        addBlocksAsRadio(screensLogs, "    Log-Screen Selection", "Select Log-Screen", "Choose a screen to display logs");
                        break;
                    case "Select Debug-Screen":
                        pro.fetchBlocks();
                        List<IMyTerminalBlock> screensDebug = pro.blocks.FindAll(x =>
                                x is IMyTextPanel
                                && x != pro.output
                                && x != pro.logs
                                && (pro.airlocks == null || !pro.airlocks.usesScreen(x))
                                && (pro.clock == null || !pro.clock.usesScreen(x))
                                && (pro.elevator == null || !pro.elevator.usesScreen(x))
                                && (pro.energy == null || !pro.energy.usesScreen(x))
                                && (pro.garage == null || !pro.garage.usesScreen(x))
                                && (pro.production == null || !pro.production.usesScreen(x))
                                && (pro.solar == null || !pro.solar.usesScreen(x)));
                        addBlocksAsRadio(screensDebug, "    Debug-Screen Selection", "Select Debug-Screen", "Choose a screen to display debug information");
                        break;
                    default:
                        pro.writeLog("the wrapper for addBlocksAsRadio didnt find its job");
                        break;
                }
            }

            //adds a list of blocks as menuobjects
            public void addBlocksAsRadio(List<IMyTerminalBlock> toAdd, String header, String name, String label)
            {
                RadioMenu radiolist = new RadioMenu(this, header, name, pro);
                radiolist.setLabel(label);
                foreach (IMyTerminalBlock block in toAdd)
                {
                    radiolist.addChild(new MenuObject(block.CustomName, block.EntityId, radiolist), block);
                }
                pro.current = radiolist;
            }

            public void addBlocksAsCheck()
            {
                switch (children[index].name)
                {
                    case "Select Ore Quota Screens":
                        pro.fetchBlocks();
                        List<IMyTerminalBlock> screensOreQuota = pro.blocks.FindAll(x =>
                                x is IMyTextPanel
                                && !pro.usesScreen(x)
                                && (pro.airlocks == null || !pro.airlocks.usesScreen(x))
                                && (pro.clock == null || !pro.clock.usesScreen(x))
                                && (pro.elevator == null || !pro.elevator.usesScreen(x))
                                && (pro.energy == null || !pro.energy.usesScreen(x))
                                && (pro.garage == null || !pro.garage.usesScreen(x))
                                && !pro.production.ingotsScreen.Contains(x)
                                && !pro.production.components1Screen.Contains(x)
                                && !pro.production.components2Screen.Contains(x)
                                && !pro.production.toolsScreen.Contains(x)
                                && !pro.production.industryScreen.Contains(x)
                                && (pro.solar == null || !pro.solar.usesScreen(x)));

                        CheckMenu menuOre = new CheckMenu(this, "   Ore Quota Screen Selection", "Select Ore Quota Screen", pro);
                        menuOre.setLabel("Choose the screens to display ore quotas");
                        foreach (IMyTerminalBlock block in screensOreQuota)
                        {
                            menuOre.addChild(new MenuObject(block.CustomName, block.EntityId, menuOre), pro.production.oresScreen.Contains(block));
                        }
                        pro.current = menuOre;
                        break;
                    case "Select Ingot Quota Screens":
                        pro.fetchBlocks();
                        List<IMyTerminalBlock> screensIngotQuota = pro.blocks.FindAll(x =>
                                x is IMyTextPanel
                                && !pro.usesScreen(x)
                                && (pro.airlocks == null || !pro.airlocks.usesScreen(x))
                                && (pro.clock == null || !pro.clock.usesScreen(x))
                                && (pro.elevator == null || !pro.elevator.usesScreen(x))
                                && (pro.energy == null || !pro.energy.usesScreen(x))
                                && (pro.garage == null || !pro.garage.usesScreen(x))
                                && !pro.production.oresScreen.Contains(x)
                                && !pro.production.components1Screen.Contains(x)
                                && !pro.production.components2Screen.Contains(x)
                                && !pro.production.toolsScreen.Contains(x)
                                && !pro.production.industryScreen.Contains(x)
                                && (pro.solar == null || !pro.solar.usesScreen(x)));

                        CheckMenu menuIngot = new CheckMenu(this, "   Ingot Quota Screen Selection", "Select Ingot Quota Screen", pro);
                        menuIngot.setLabel("Choose the screens to display ingot quotas");
                        foreach (IMyTerminalBlock block in screensIngotQuota)
                        {
                            menuIngot.addChild(new MenuObject(block.CustomName, block.EntityId, menuIngot), pro.production.ingotsScreen.Contains(block));
                        }
                        pro.current = menuIngot;
                        break;
                    case "Select Component1 Quota Screens":
                        pro.fetchBlocks();
                        List<IMyTerminalBlock> screensComp1Quota = pro.blocks.FindAll(x =>
                                x is IMyTextPanel
                                && !pro.usesScreen(x)
                                && (pro.airlocks == null || !pro.airlocks.usesScreen(x))
                                && (pro.clock == null || !pro.clock.usesScreen(x))
                                && (pro.elevator == null || !pro.elevator.usesScreen(x))
                                && (pro.energy == null || !pro.energy.usesScreen(x))
                                && (pro.garage == null || !pro.garage.usesScreen(x))
                                && !pro.production.oresScreen.Contains(x)
                                && !pro.production.ingotsScreen.Contains(x)
                                && !pro.production.components2Screen.Contains(x)
                                && !pro.production.toolsScreen.Contains(x)
                                && !pro.production.industryScreen.Contains(x)
                                && (pro.solar == null || !pro.solar.usesScreen(x)));

                        CheckMenu menuComp1 = new CheckMenu(this, "   Component1 Quota Screen Selection", "Select Component1 Quota Screen", pro);
                        menuComp1.setLabel("Choose the screens to display component1 quotas");
                        foreach (IMyTerminalBlock block in screensComp1Quota)
                        {
                            menuComp1.addChild(new MenuObject(block.CustomName, block.EntityId, menuComp1), pro.production.components1Screen.Contains(block));
                        }
                        pro.current = menuComp1;
                        break;
                    case "Select Component2 Quota Screens":
                        pro.fetchBlocks();
                        List<IMyTerminalBlock> screensComp2Quota = pro.blocks.FindAll(x =>
                                x is IMyTextPanel
                                && !pro.usesScreen(x)
                                && (pro.airlocks == null || !pro.airlocks.usesScreen(x))
                                && (pro.clock == null || !pro.clock.usesScreen(x))
                                && (pro.elevator == null || !pro.elevator.usesScreen(x))
                                && (pro.energy == null || !pro.energy.usesScreen(x))
                                && (pro.garage == null || !pro.garage.usesScreen(x))
                                && !pro.production.oresScreen.Contains(x)
                                && !pro.production.ingotsScreen.Contains(x)
                                && !pro.production.components1Screen.Contains(x)
                                && !pro.production.toolsScreen.Contains(x)
                                && !pro.production.industryScreen.Contains(x)
                                && (pro.solar == null || !pro.solar.usesScreen(x)));

                        CheckMenu menuComp2 = new CheckMenu(this, "   Component2 Quota Screen Selection", "Select Component2 Quota Screen", pro);
                        menuComp2.setLabel("Choose the screens to display component2 quotas");
                        foreach (IMyTerminalBlock block in screensComp2Quota)
                        {
                            menuComp2.addChild(new MenuObject(block.CustomName, block.EntityId, menuComp2), pro.production.components2Screen.Contains(block));
                        }
                        pro.current = menuComp2;
                        break;
                    case "Select Tools Quota Screens":
                        pro.fetchBlocks();
                        List<IMyTerminalBlock> screensToolsQuota = pro.blocks.FindAll(x =>
                                x is IMyTextPanel
                                && !pro.usesScreen(x)
                                && (pro.airlocks == null || !pro.airlocks.usesScreen(x))
                                && (pro.clock == null || !pro.clock.usesScreen(x))
                                && (pro.elevator == null || !pro.elevator.usesScreen(x))
                                && (pro.energy == null || !pro.energy.usesScreen(x))
                                && (pro.garage == null || !pro.garage.usesScreen(x))
                                && !pro.production.oresScreen.Contains(x)
                                && !pro.production.ingotsScreen.Contains(x)
                                && !pro.production.components1Screen.Contains(x)
                                && !pro.production.components2Screen.Contains(x)
                                && !pro.production.industryScreen.Contains(x)
                                && (pro.solar == null || !pro.solar.usesScreen(x)));

                        CheckMenu menuTools = new CheckMenu(this, "   Tools Quota Screen Selection", "Select Tools Quota Screen", pro);
                        menuTools.setLabel("Choose the screens to display tools quotas");
                        foreach (IMyTerminalBlock block in screensToolsQuota)
                        {
                            menuTools.addChild(new MenuObject(block.CustomName, block.EntityId, menuTools), pro.production.toolsScreen.Contains(block));
                        }
                        pro.current = menuTools;
                        break;
                    case "Select Industry Status Screens":
                        pro.fetchBlocks();
                        List<IMyTerminalBlock> screensIndustryQuota = pro.blocks.FindAll(x =>
                                x is IMyTextPanel
                                && !pro.usesScreen(x)
                                && (pro.airlocks == null || !pro.airlocks.usesScreen(x))
                                && (pro.clock == null || !pro.clock.usesScreen(x))
                                && (pro.elevator == null || !pro.elevator.usesScreen(x))
                                && (pro.energy == null || !pro.energy.usesScreen(x))
                                && (pro.garage == null || !pro.garage.usesScreen(x))
                                && !pro.production.oresScreen.Contains(x)
                                && !pro.production.ingotsScreen.Contains(x)
                                && !pro.production.components1Screen.Contains(x)
                                && !pro.production.components2Screen.Contains(x)
                                && !pro.production.toolsScreen.Contains(x)
                                && (pro.solar == null || !pro.solar.usesScreen(x)));

                        CheckMenu menuIndustry = new CheckMenu(this, "   Industry Status Screen Selection", "Select Industry Status Screen", pro);
                        menuIndustry.setLabel("Choose the screens to display tools quotas");
                        foreach (IMyTerminalBlock block in screensIndustryQuota)
                        {
                            menuIndustry.addChild(new MenuObject(block.CustomName, block.EntityId, menuIndustry), pro.production.toolsScreen.Contains(block));
                        }
                        pro.current = menuIndustry;
                        break;
                    default:
                        pro.writeLog("The wrapper for addBlocksAsChecks didnt find its job");
                        break;

                }
            }


            //navigation and printing
            public void up()
            {
                //pro.writeLog("up triggered! name: " + name, false);
                if (index != 0) index--;
                else index = children.Count - 1;
                pro.reprintMenu();
            }

            public void down()
            {
                //pro.writeLog("down triggered! name: " + name, false);
                if (index != children.Count - 1) index++;
                else index = 0;
                pro.reprintMenu();
            }

            public void back()
            {
                pro.current = (parent == null ? this : parent);
                pro.reprintMenu();
            }

            virtual public void enter()
            {
                MenuObject selected = children[index];
                if (selected is Menu)
                {
                    pro.current = (Menu)selected;
                }
                else
                {
                    ((MenuAction)selected).invokeAction();
                }
                pro.reprintMenu();
            }

            virtual public String toString()
            {
                String result = header + "\n\n  ";

                if (label != null)
                {
                    foreach (String line in label.Split(Environment.NewLine.ToCharArray()))
                    {
                        result += line + "\n  ";
                    }

                }

                for (int i = 0; i < children.Count; i++)
                {
                    result += "\n " + (i == index ? "> " : "   ") + children[i].name;
                }

                return result;
            }
        }

        class RadioMenu : Menu
        {
            int radioSelected;


            public RadioMenu(Menu parent, String header, String name, Program pro) : base(parent, header, name, pro)
            {
                radioSelected = -1;
            }

            override public String toString()
            {
                String result = header + "\n\n  ";

                if (label != null)
                {
                    foreach (String line in label.Split(Environment.NewLine.ToCharArray()))
                    {
                        result += line + "\n  ";
                    }

                }

                for (int i = 0; i < children.Count; i++)
                {
                    result += "\n " + (i == index ? "> " : "   ") + (i == radioSelected ? "[x] " : "[  ] ") + children[i].name;
                }

                return result;
            }

            public void addChild(MenuObject value, IMyTerminalBlock block)
            {
                children.Add(value);
                switch (name)
                {
                    case "Select Log-Screen":
                        if (pro.logs == block) radioSelected = children.Count - 1;
                        break;
                    case "Select Debug-Screen":
                        if (pro.debug == block) radioSelected = children.Count - 1;
                        break;
                    default:
                        pro.writeLog("RadioMenu:addChild did not recognize name");
                        break;
                }
            }

            override public void enter()
            {
                //if we press enter on the already selected MenuObject it should be deselected
                radioSelected = (radioSelected == index ? -1 : index);

                switch (name)
                {
                    case "Select Log-Screen":
                        if (radioSelected >= 0)
                        {
                            IMyTerminalBlock temp = pro.idToBlock(children[index].entityId);
                            if (temp != null)
                            {
                                if (pro.logs != null) pro.logs.WritePublicText(""); //clearing the old screen
                                pro.logs = (IMyTextPanel)temp;
                                pro.logs.FontSize = 0.8F;
                                pro.logs.ShowPublicTextOnScreen();
                                pro.printIni();
                                pro.printIni(true);
                            }
                        }
                        else
                        {
                            pro.logs.WritePublicText(""); //clearing the old screen
                            pro.logs = null;
                            pro.printIni();
                            pro.printIni(true);
                        }
                        break;
                    case "Select Debug-Screen":
                        if (radioSelected >= 0)
                        {
                            IMyTerminalBlock temp = pro.idToBlock(children[index].entityId);
                            if (temp != null)
                            {
                                if (pro.debug != null) pro.debug.WritePublicText(""); //clearing the old screen
                                pro.debug = (IMyTextPanel)temp;
                                pro.debug.FontSize = 0.8F;
                                pro.debug.ShowPublicTextOnScreen();
                                pro.printIni();
                                pro.printIni(true);
                            }
                        }
                        else
                        {
                            pro.debug.WritePublicText(""); //clearing the old screen
                            pro.debug = null;
                            pro.printIni();
                            pro.printIni(true);
                        }
                        break;
                    default:
                        pro.writeLog("RadioMenu:enter did not recognize name");
                        break;
                }
                pro.reprintMenu();
            }
        }

        class CheckMenu : Menu
        {
            List<bool> checks;

            public CheckMenu(Menu parent, String header, String name, Program pro) : base(parent, header, name, pro)
            {
                checks = new List<bool>();
            }

            public override string toString()
            {
                pro.writeLog("Checkmenu::toString() triggered");
                String result = header + "\n\n  ";

                if (label != null)
                {
                    foreach (String line in label.Split(Environment.NewLine.ToCharArray()))
                    {
                        result += line + "\n  ";
                    }
                }

                for (int i = 0; i < children.Count; i++)
                {
                    result += "\n" + (i == index ? "> " : "   ") + (checks[i] ? "[x] " : "[  ] ") + children[i].name;
                }

                return result;
            }

            public void addChild(MenuObject value, bool isSelected)
            {
                children.Add(value);
                checks.Add(isSelected);
                /* for some reason there once was a switch. keeping it in case i soon think it is necessary
                switch (name)
                {
                    case "Select Functionalities":
                        checks.Add(isSelected);
                        break;
                    default:
                        pro.writeLog("CheckMenu::addChild did not recognize name");
                        break;
                }*/
            }

            public override void enter()
            {
                switch (name)
                {
                    case "Select Functionalities":
                        String itemName = children[index].name;
                        switch (itemName)
                        {
                            //TODO add remove to main menu to remove functionality as menu point there
                            case "Production Management":
                                pro.enableProductionManagement = !pro.enableProductionManagement;
                                pro.production = (pro.enableProductionManagement ? new ProductionManagement(pro) : null);
                                checks[index] = pro.enableProductionManagement;
                                break;
                            case "Energy Management":
                                pro.enableEnergyManagement = !pro.enableEnergyManagement;
                                pro.energy = (pro.enableEnergyManagement ? new EnergyManagement(pro) : null);
                                checks[index] = pro.enableEnergyManagement;
                                break;
                            case "Airlocks":
                                pro.enableAirlocks = !pro.enableAirlocks;
                                pro.airlocks = (pro.enableAirlocks ? new Airlocks(pro) : null);
                                checks[index] = pro.enableAirlocks;
                                break;
                            case "Elevator":
                                pro.enableElevator = !pro.enableElevator;
                                pro.elevator = (pro.enableElevator ? new Elevator(pro) : null);
                                checks[index] = pro.enableElevator;
                                break;
                            case "Garage Management":
                                pro.enableGarageManagement = !pro.enableGarageManagement;
                                pro.garage = (pro.enableGarageManagement ? new GarageManagement(pro) : null);
                                checks[index] = pro.enableGarageManagement;
                                break;
                            case "Solarpanel Adjustment":
                                pro.enableSolarpanelAdjustment = !pro.enableGarageManagement;
                                pro.solar = (pro.enableSolarpanelAdjustment ? new SolarAdjustment(pro) : null);
                                checks[index] = pro.enableGarageManagement;
                                break;
                            default:
                                pro.writeLog("CheckMenu::enter() - did not recognize itemName: " + itemName);
                                break;
                        }
                        break;
                    case "Select Ore Quota Screen":
                        IMyTextPanel oreScreen = (IMyTextPanel)pro.idToBlock(children[index].entityId);
                        if (pro.production.oresScreen.Contains(oreScreen))
                        {
                            pro.production.oresScreen.Remove(oreScreen);
                            checks[index] = false;
                        }
                        else
                        {
                            pro.production.oresScreen.Add(oreScreen);
                            checks[index] = true;
                        }
                        break;
                    case "Select Ingot Quota Screen":
                        IMyTextPanel ingotScreen = (IMyTextPanel)pro.idToBlock(children[index].entityId);
                        if (pro.production.ingotsScreen.Contains(ingotScreen))
                        {
                            pro.production.ingotsScreen.Remove(ingotScreen);
                            checks[index] = false;
                        }
                        else
                        {
                            pro.production.ingotsScreen.Add(ingotScreen);
                            checks[index] = true;
                        }
                        break;
                    case "Select Component1 Quota Screen":
                        IMyTextPanel comp1Screen = (IMyTextPanel)pro.idToBlock(children[index].entityId);
                        if (pro.production.components1Screen.Contains(comp1Screen))
                        {
                            pro.production.components1Screen.Remove(comp1Screen);
                            checks[index] = false;
                        }
                        else
                        {
                            pro.production.components1Screen.Add(comp1Screen);
                            checks[index] = true;
                        }
                        break;
                    case "Select Component2 Quota Screen":
                        IMyTextPanel comp2Screen = (IMyTextPanel)pro.idToBlock(children[index].entityId);
                        if (pro.production.components2Screen.Contains(comp2Screen))
                        {
                            pro.production.components2Screen.Remove(comp2Screen);
                            checks[index] = false;
                        }
                        else
                        {
                            pro.production.components2Screen.Add(comp2Screen);
                            checks[index] = true;
                        }
                        break;
                    case "Select Tools Quota Screen":
                        IMyTextPanel toolsScreen = (IMyTextPanel)pro.idToBlock(children[index].entityId);
                        if (pro.production.toolsScreen.Contains(toolsScreen))
                        {
                            pro.production.toolsScreen.Remove(toolsScreen);
                            checks[index] = false;
                        }
                        else
                        {
                            pro.production.toolsScreen.Add(toolsScreen);
                            checks[index] = true;
                        }
                        break;
                    case "Select Industry Status Screen":
                        IMyTextPanel industryScreen = (IMyTextPanel)pro.idToBlock(children[index].entityId);
                        if (pro.production.industryScreen.Contains(industryScreen))
                        {
                            pro.production.industryScreen.Remove(industryScreen);
                            checks[index] = false;
                        }
                        else
                        {
                            pro.production.industryScreen.Add(industryScreen);
                            checks[index] = true;
                        }
                        break;
                    default:
                        pro.writeLog("CheckMenu::enter did not recognize name: " + name);
                        break;
                }
                pro.reprintMenu();
            }
        }

        class MenuAction : MenuObject
        {
            Action action;

            //constructors
            public MenuAction(Menu parent, String name, Action action, Program pro) : base(name, pro, parent)
            {
                //TODO: this
                this.action = action;
            }


            //getters
            public Action getAction() { return action; }

            public void invokeAction() { action.Invoke(); }

        }


        /* * * * * * * * * * * * * *
         * Further Functionalities *
         * * * * * * * * * * * * * */

        class Extension
        {
            protected Program pro;

            public Extension(Program pro)
            {
                this.pro = pro;
            }

            virtual public void run()
            {

            }

            virtual public bool usesScreen(IMyTerminalBlock panel)
            {
                pro.writeLog("usesScreen triggered!", true);
                return false;
            }
        }

        class Airlocks : Extension
        {


            public Airlocks(Program pro) : base(pro)
            {

            }
        }

        class Clock : Extension
        {


            public Clock(Program pro) : base(pro)
            {

            }
        }

        class Elevator : Extension
        {


            public Elevator(Program pro) : base(pro)
            {

            }

        }

        class EnergyManagement : Extension
        {
            /* TODO:
             * Make engines turn off below a certain hydrogen threshhold in system
             * 
             * make commands to toggle engines manually
             * 
             * add power save mode
             * 
             * 
             */
            public List<IMyBatteryBlock> batteries;
            public List<IMyReactor> reactors;
            public List<IMyPowerProducer> engines;

            public EnergyManagement(Program pro) : base(pro)
            {

            }

            public override void run()
            {

            }

            public void fetchBlocks()
            {
                pro.fetchBlocks();

                batteries.Clear();
                reactors.Clear();
                engines.Clear();
                foreach (IMyTerminalBlock block in pro.blocks)
                {
                    if (block is IMyPowerProducer)
                    {
                        if (block is IMyBatteryBlock) batteries.Add((IMyBatteryBlock)block);
                        else if (block is IMyReactor) reactors.Add((IMyReactor)block);
                        else if (!(block is IMySolarPanel) && block.BlockDefinition.TypeIdString == "MyObjectBuilder_HydrogenEngine") engines.Add((IMyPowerProducer)block);
                    }
                }
            }



        }

        class GarageManagement : Extension
        {


            public GarageManagement(Program pro) : base(pro)
            {

            }
        }

        class ProductionManagement : Extension
        {
            /*
            send content of chests via antenna
            *      content display of whole base
            *          -> set production quota
            *              -> font size for 27 lines
            *              screen font size 1.1 fits 13 lines, which is exactly what we need
            *
            *
            *
            * display additional ores required for fulfillment of quota
            *
            * eject everything beyond a certain quota of defined blocks
            *          
            *          stack items in chest
            * take care of filling of refineries
            *          -> sort ores chests based on refinery speed
            *              -> fill refineries depending on speed/yield modules
            *              -> set preference for speed or yield modules
            *              -> display speed or yield seperatly with remaining time
            *              -> yield may help speed module refineries until yield materials are back, not the other way around, displayed as (helping)
            * ejector management
            *          -> if more than quota of component, ingot or ore is present, eject it via predefined connector.
            */

            public List<IMyTextPanel> oresScreen;
            public List<IMyTextPanel> ingotsScreen;
            public List<IMyTextPanel> components1Screen;
            public List<IMyTextPanel> components2Screen;
            public List<IMyTextPanel> toolsScreen;
            public List<IMyTextPanel> industryScreen;

            //this is required to check wether or not the quotas have changed
            String contentOres;
            String contentIngots;
            String contentComp1;
            String contentComp2;
            String contentTools;

            List<IMyTerminalBlock> inventoryBlocks;
            List<IMyCargoContainer> containers;
            List<IMyAssembler> assemblers;
            List<IMyRefinery> refineries;
            List<IMyTerminalBlock> restBlocks;

            bool refHelp;
            String refHelpText = "";
            String assWorkText = "";
            int stacksize = 200;                   //TODO: make this user configurable

            Dictionary<String, float> ores;
            Dictionary<String, float> ingots;
            Dictionary<String, float> components;
            Dictionary<String, float> rest;

            Dictionary<String, float> oresQ;
            Dictionary<String, float> ingotsQ;
            Dictionary<String, float> componentsQ;
            Dictionary<String, float> restQ;

            List<difference> oresD;
            List<difference> ingotsD;
            List<difference> componentsD;
            List<difference> restD;

            //runtimes for debugscreen
            public int runtimeFetch;
            public int runtimeAnalyze;
            public int runtimeCalculate;
            public int runtimeFillAssemblers;
            public int runtimeFillRefineries;

            public ProductionManagement(Program pro) : base(pro)
            {
                oresScreen = new List<IMyTextPanel>();
                ingotsScreen = new List<IMyTextPanel>();
                components1Screen = new List<IMyTextPanel>();
                components2Screen = new List<IMyTextPanel>();
                toolsScreen = new List<IMyTextPanel>();
                industryScreen = new List<IMyTextPanel>();

                inventoryBlocks = new List<IMyTerminalBlock>(); //without assemblers or refineries
                containers = new List<IMyCargoContainer>();
                assemblers = new List<IMyAssembler>();
                refineries = new List<IMyRefinery>();
                restBlocks = new List<IMyTerminalBlock>();

                //inventories
                ores = new Dictionary<string, float>();
                ingots = new Dictionary<string, float>();
                components = new Dictionary<string, float>();
                rest = new Dictionary<string, float>();

                //quotas
                oresQ = new Dictionary<string, float>();
                ingotsQ = new Dictionary<string, float>();
                componentsQ = new Dictionary<string, float>();
                restQ = new Dictionary<string, float>();

                //differences
                oresD = new List<difference>();
                ingotsD = new List<difference>();
                componentsD = new List<difference>();
                restD = new List<difference>();

                fetchBlocks();
            }

            //Temporary method that does all of production management in one tick. this shall be split up into several ticks later, once everything works
            public override void run()
            {
                //pro.writeLog("Production Management is running!");
                analyzeInventories();

                //analyze and draw quotas
                drawOres();
                drawIngots();
                drawComponents1();
                drawComponents2();
                drawToolsQuota();

                //calculate differences between quotas and inventories
                calculateDifferences();

                //fill assembler todo lists with differences / number of assemblers based on ratio least fulfilled
                fillAssemblers();
                //sort difference dictionaries based on value and sort assemblers according to percentage of least amount.
                //if the inventory of the assemblers are fuller than a certain threshold, half the biggest stack in it.

                fillRefineries();

                //iterate through todo lists until one piece is missing ingots, fill refineries with that ore / number of refineries

                drawIndustryStatusScreen();


            }

            public override bool usesScreen(IMyTerminalBlock panel)
            {
                pro.writeLog("production uses Screen triggered", true);
                return oresScreen.Contains(panel)
                    || ingotsScreen.Contains(panel)
                    || components1Screen.Contains(panel)
                    || components2Screen.Contains(panel)
                    || toolsScreen.Contains(panel)
                    || industryScreen.Contains(panel);
            }

            void analyzeInventories()
            {
                int currInst = pro.Runtime.CurrentInstructionCount;
                if (inventoryBlocks.Count != 0)
                {
                    components.Clear();
                    ingots.Clear();
                    ores.Clear();
                    rest.Clear();
                    foreach (IMyTerminalBlock block in inventoryBlocks)
                    {
                        for (int i = 0; i < block.InventoryCount; i++)
                        {
                            List<MyInventoryItem> items = new List<MyInventoryItem>();
                            block.GetInventory(i).GetItems(items);
                            foreach (MyInventoryItem item in items)
                            {
                                String key = item.Type.SubtypeId;
                                float valueFloat;
                                switch (item.Type.TypeId)
                                {
                                    case "MyObjectBuilder_Component":
                                        if (components.TryGetValue(key, out valueFloat))
                                        {
                                            components[key] = valueFloat + (int)item.Amount;
                                        }
                                        else components.Add(key, (int)item.Amount);
                                        break;
                                    case "MyObjectBuilder_Ingot":
                                        if (ingots.TryGetValue(key, out valueFloat))
                                        {
                                            ingots[key] = valueFloat + (float)item.Amount;
                                        }
                                        else ingots.Add(key, (float)item.Amount);
                                        break;
                                    case "MyObjectBuilder_Ore":
                                        if (ores.TryGetValue(key, out valueFloat))
                                        {
                                            ores[key] = valueFloat + (float)item.Amount;
                                        }
                                        else ores.Add(key, (float)item.Amount);
                                        break;
                                    default:
                                        if (rest.TryGetValue(key, out valueFloat))
                                        {
                                            rest[key] = valueFloat + (int)item.Amount;
                                        }
                                        else rest.Add(key, (int)item.Amount);
                                        break;
                                }
                            }
                        }
                    }
                }

                runtimeAnalyze = pro.Runtime.CurrentInstructionCount - currInst;
            }

            void calculateDifferences()
            {
                int currInst = pro.Runtime.CurrentInstructionCount;
                //ores
                oresD.Clear();
                foreach (String key in oresQ.Keys.ToArray())
                {
                    float current = 0;
                    float quota = 0;
                    ores.TryGetValue(key, out current);
                    oresQ.TryGetValue(key, out quota);
                    float difference = quota - current;
                    if (difference > 0)
                    {
                        oresD.Add(new difference(key, difference, current / quota));
                    }
                }
                oresD = oresD.OrderBy(d => d.ratio).ToList();

                //ingots
                ingotsD.Clear();
                foreach (String key in ingotsQ.Keys.ToArray())
                {
                    float current = 0;
                    float quota = 0;
                    ingots.TryGetValue(key, out current);
                    ingotsQ.TryGetValue(key, out quota);
                    float difference = quota - current;
                    if (difference > 0)
                    {
                        ingotsD.Add(new difference(key, difference, current / quota));
                    }
                }
                ingotsD = ingotsD.OrderBy(d => d.ratio).ToList();

                //components&rest
                componentsD.Clear();
                foreach (String key in componentsQ.Keys.ToArray())
                {
                    float current = 0;
                    float quota = 0;
                    components.TryGetValue(key, out current);
                    componentsQ.TryGetValue(key, out quota);
                    float difference = quota - current;
                    if (difference > 0)
                    {
                        componentsD.Add(new difference(key, difference, current / quota));
                    }
                }
                componentsD = componentsD.OrderBy(d => d.ratio).ToList();

                restD.Clear();
                foreach (String key in restQ.Keys.ToArray())
                {
                    float current = 0;
                    float quota = 0;
                    rest.TryGetValue(key, out current);
                    restQ.TryGetValue(key, out quota);
                    float difference = quota - current;
                    if (difference > 0)
                    {
                        restD.Add(new difference(key, difference, current / quota));
                    }
                }
                restD = restD.OrderBy(d => d.ratio).ToList();

                runtimeCalculate = pro.Runtime.CurrentInstructionCount - currInst;
            }

            void fillAssemblers()
            {
                int currInst = pro.Runtime.CurrentInstructionCount;
                for (int i = 0; i < 5 && i < componentsD.Count; i++)
                {
                    difference toCraft = componentsD[i];
                    int delta;
                    float maxDelta = toCraft.diff + (componentsQ[toCraft.key] / 50);

                    //if the total amount of components to be produced by the assemblers is larger than what we need, then calculate what we need to add to fit the quota
                    if (stacksize * assemblers.Count > maxDelta)
                    {
                        delta = (int)maxDelta / assemblers.Count;
                    }
                    else delta = stacksize;

                    foreach (IMyAssembler ass in assemblers)
                    {
                        if (ass.IsSameConstructAs(pro.me))
                        {
                            List<MyProductionItem> queue = new List<MyProductionItem>();
                            MyDefinitionId? toAdd = CreateBlueprint(toCraft.key);

                            if (toAdd != null)
                            {
                                ass.GetQueue(queue);
                                if (!queue.Exists(x => x.BlueprintId.SubtypeId == toAdd.Value.SubtypeId))
                                {
                                    ass.InsertQueueItem(i, (MyDefinitionId)toAdd, (decimal)delta);
                                }
                            }
                        }
                    }
                }
                runtimeFillAssemblers = pro.Runtime.CurrentInstructionCount - currInst;
            }

            void fillRefineries()
            {
                int currInst = pro.Runtime.CurrentInstructionCount;

                runtimeFillRefineries = pro.Runtime.CurrentInstructionCount - currInst;
            }

            private static MyDefinitionId? CreateBlueprint(string name)
            {
                switch (name)
                {
                    case "RadioCommunication":
                    case "Computer":
                    case "Reactor":
                    case "Detector":
                    case "Construction":
                    case "Thrust":
                    case "Motor":
                    case "Explosives":
                    case "Girder":
                    case "GravityGenerator":
                    case "Medical": name += "Component"; break;
                    case "NATO_25x184mm":
                    case "NATO_5p56x45mm": name += "Magazine"; break;
                }
                MyDefinitionId id;
                if (MyDefinitionId.TryParse("MyObjectBuilder_BlueprintDefinition/" + name, out id) && (id.SubtypeId != null))
                    return id;
                else
                    return null;
            }

            public void fetchBlocks()
            {
                int currInst = pro.Runtime.CurrentInstructionCount;
                pro.fetchBlocks();

                inventoryBlocks.Clear();
                containers.Clear();
                assemblers.Clear();
                refineries.Clear();
                restBlocks.Clear();
                foreach (IMyTerminalBlock block in pro.blocks)
                {
                    if (block.HasInventory)
                    {
                        inventoryBlocks.Add(block);
                        if (block is IMyCargoContainer) containers.Add((IMyCargoContainer)block);
                        else if (block is IMyAssembler) assemblers.Add((IMyAssembler)block);
                        else if (block is IMyRefinery) refineries.Add((IMyRefinery)block);
                        else restBlocks.Add(block);
                    }
                }

                //TODO: delete this after some testing
                runtimeFetch = pro.Runtime.CurrentInstructionCount - currInst;
            }

            //quota drawers
            public void drawOres()
            {
                if (oresScreen.Count != 0)
                {
                    foreach (IMyTextPanel screen in oresScreen)
                    {
                        if (screen.GetText() != contentOres)
                        {
                            String[] lines = screen.GetText().Split('\n');

                            if (lines.Length == 15)
                            {
                                int parseResult;
                                pro.parseNumber(lines[3].Split('/')[1], out parseResult);
                                oresQ["Stone"] = parseResult;
                                pro.parseNumber(lines[4].Split('/')[1], out parseResult);
                                oresQ["Iron"] = parseResult;
                                pro.parseNumber(lines[5].Split('/')[1], out parseResult);
                                oresQ["Nickel"] = parseResult;
                                pro.parseNumber(lines[6].Split('/')[1], out parseResult);
                                oresQ["Cobalt"] = parseResult;
                                pro.parseNumber(lines[7].Split('/')[1], out parseResult);
                                oresQ["Magnesium"] = parseResult;
                                pro.parseNumber(lines[8].Split('/')[1], out parseResult);
                                oresQ["Silicon"] = parseResult;
                                pro.parseNumber(lines[9].Split('/')[1], out parseResult);
                                oresQ["Silver"] = parseResult;
                                pro.parseNumber(lines[10].Split('/')[1], out parseResult);
                                oresQ["Gold"] = parseResult;
                                pro.parseNumber(lines[11].Split('/')[1], out parseResult);
                                oresQ["Platinum"] = parseResult;
                                pro.parseNumber(lines[12].Split('/')[1], out parseResult);
                                oresQ["Uranium"] = parseResult;
                                pro.parseNumber(lines[13].Split('/')[1], out parseResult);
                                oresQ["Ice"] = parseResult;
                                pro.parseNumber(lines[14].Split('/')[1], out parseResult);
                                oresQ["Scrap"] = parseResult;
                            }
                            else
                            {
                                pro.writeLog("length of lines did not match! length: " + lines.Length);
                            }
                        }
                    }

                    //printing new content
                    contentOres = "\n        Ores quota\n";
                    addLine(ref contentOres, "Stone                 ", ores, oresQ, "Stone");
                    addLine(ref contentOres, "Iron                  ", ores, oresQ, "Iron");
                    addLine(ref contentOres, "Nickel                ", ores, oresQ, "Nickel");
                    addLine(ref contentOres, "Cobalt                ", ores, oresQ, "Cobalt");
                    addLine(ref contentOres, "Magnesium             ", ores, oresQ, "Magnesium");
                    addLine(ref contentOres, "Silicon               ", ores, oresQ, "Silicon");
                    addLine(ref contentOres, "Silver                ", ores, oresQ, "Silver");
                    addLine(ref contentOres, "Gold                  ", ores, oresQ, "Gold");
                    addLine(ref contentOres, "Platinum              ", ores, oresQ, "Platinum");
                    addLine(ref contentOres, "Uranium               ", ores, oresQ, "Uranium");
                    addLine(ref contentOres, "Ice                   ", ores, oresQ, "Ice");
                    addLine(ref contentOres, "Scrap                 ", ores, oresQ, "Scrap");

                    drawStringOnScreens(oresScreen, contentOres);
                }
            }

            public void drawIngots()
            {
                if (ingotsScreen.Count != 0)
                {
                    foreach (IMyTextPanel screen in ingotsScreen)
                    {
                        if (screen.GetText() != contentIngots)
                        {
                            String[] lines = screen.GetText().Split('\n');

                            if (lines.Length == 13)
                            {
                                int parseResult;
                                pro.parseNumber(lines[3].Split('/')[1], out parseResult);
                                ingotsQ["Gravel"] = parseResult;
                                pro.parseNumber(lines[4].Split('/')[1], out parseResult);
                                ingotsQ["Iron"] = parseResult;
                                pro.parseNumber(lines[5].Split('/')[1], out parseResult);
                                ingotsQ["Nickel"] = parseResult;
                                pro.parseNumber(lines[6].Split('/')[1], out parseResult);
                                ingotsQ["Cobalt"] = parseResult;
                                pro.parseNumber(lines[7].Split('/')[1], out parseResult);
                                ingotsQ["Magnesium"] = parseResult;
                                pro.parseNumber(lines[8].Split('/')[1], out parseResult);
                                ingotsQ["Silicon"] = parseResult;
                                pro.parseNumber(lines[9].Split('/')[1], out parseResult);
                                ingotsQ["Silver"] = parseResult;
                                pro.parseNumber(lines[10].Split('/')[1], out parseResult);
                                ingotsQ["Gold"] = parseResult;
                                pro.parseNumber(lines[11].Split('/')[1], out parseResult);
                                ingotsQ["Platinum"] = parseResult;
                                pro.parseNumber(lines[12].Split('/')[1], out parseResult);
                                ingotsQ["Uranium"] = parseResult;
                            }
                        }
                    }

                    //printing new content
                    contentIngots = "\n          Ingots quota\n";
                    addLine(ref contentIngots, "Gravel                ", ingots, ingotsQ, "Stone");
                    addLine(ref contentIngots, "Iron                  ", ingots, ingotsQ, "Iron");
                    addLine(ref contentIngots, "Nickel                ", ingots, ingotsQ, "Nickel");
                    addLine(ref contentIngots, "Cobalt                ", ingots, ingotsQ, "Cobalt");
                    addLine(ref contentIngots, "Magnesium             ", ingots, ingotsQ, "Magnesium");
                    addLine(ref contentIngots, "Silicon               ", ingots, ingotsQ, "Silicon");
                    addLine(ref contentIngots, "Silver                ", ingots, ingotsQ, "Silver");
                    addLine(ref contentIngots, "Gold                  ", ingots, ingotsQ, "Gold");
                    addLine(ref contentIngots, "Platinum              ", ingots, ingotsQ, "Platinum");
                    addLine(ref contentIngots, "Uranium               ", ingots, ingotsQ, "Uranium");
                    drawStringOnScreens(ingotsScreen, contentIngots);
                }
            }

            public void drawComponents1()
            {
                //TODO: add information wether or not the quota can actually fit the storage space
                if (components1Screen.Count != 0)
                {
                    foreach (IMyTextPanel screen in components1Screen)
                    {
                        if (screen.GetText() != contentComp1)
                        {
                            String[] lines = screen.GetText().Split('\n');

                            if (lines.Length == 17)
                            {
                                int parseResult;
                                pro.parseNumber(lines[3].Split('/')[1], out parseResult);
                                componentsQ["SteelPlate"] = parseResult;
                                pro.parseNumber(lines[4].Split('/')[1], out parseResult);
                                componentsQ["InteriorPlate"] = parseResult;
                                pro.parseNumber(lines[5].Split('/')[1], out parseResult);
                                componentsQ["Construction"] = parseResult;
                                pro.parseNumber(lines[6].Split('/')[1], out parseResult);
                                componentsQ["MetalGrid"] = parseResult;
                                pro.parseNumber(lines[7].Split('/')[1], out parseResult);
                                componentsQ["SmallTube"] = parseResult;
                                pro.parseNumber(lines[8].Split('/')[1], out parseResult);
                                componentsQ["LargeTube"] = parseResult;
                                pro.parseNumber(lines[9].Split('/')[1], out parseResult);
                                componentsQ["Girder"] = parseResult;
                                pro.parseNumber(lines[10].Split('/')[1], out parseResult);
                                componentsQ["BulletproofGlass"] = parseResult;
                                pro.parseNumber(lines[11].Split('/')[1], out parseResult);
                                componentsQ["Motor"] = parseResult;
                                pro.parseNumber(lines[12].Split('/')[1], out parseResult);
                                componentsQ["Display"] = parseResult;
                                pro.parseNumber(lines[13].Split('/')[1], out parseResult);
                                componentsQ["Computer"] = parseResult;
                                pro.parseNumber(lines[14].Split('/')[1], out parseResult);
                                componentsQ["PowerCell"] = parseResult;
                                pro.parseNumber(lines[15].Split('/')[1], out parseResult);
                                componentsQ["SolarCell"] = parseResult;
                                pro.parseNumber(lines[16].Split('/')[1], out parseResult);
                                componentsQ["Reactor"] = parseResult;
                            }
                        }
                    }

                    //printing new content
                    contentComp1 = "\n        Components1 quota\n";
                    addLine(ref contentComp1, "Steel Plate           ", components, componentsQ, "SteelPlate");
                    addLine(ref contentComp1, "Interior Plate        ", components, componentsQ, "InteriorPlate");
                    addLine(ref contentComp1, "Construction          ", components, componentsQ, "Construction");
                    addLine(ref contentComp1, "Metal Grid            ", components, componentsQ, "MetalGrid");
                    addLine(ref contentComp1, "Small Tube            ", components, componentsQ, "SmallTube");
                    addLine(ref contentComp1, "Large Tube            ", components, componentsQ, "LargeTube");
                    addLine(ref contentComp1, "Girder                ", components, componentsQ, "Girder");
                    addLine(ref contentComp1, "Bulletproof Glass     ", components, componentsQ, "BulletproofGlass");
                    addLine(ref contentComp1, "Motor                 ", components, componentsQ, "Motor");
                    addLine(ref contentComp1, "Display               ", components, componentsQ, "Display");
                    addLine(ref contentComp1, "Computer              ", components, componentsQ, "Computer");
                    addLine(ref contentComp1, "Power Cell            ", components, componentsQ, "PowerCell");
                    addLine(ref contentComp1, "Solar Cell            ", components, componentsQ, "SolarCell");
                    addLine(ref contentComp1, "Reactor               ", components, componentsQ, "Reactor");
                    drawStringOnScreens(components1Screen, contentComp1);
                }
            }

            public void drawComponents2()
            {
                if (components2Screen.Count != 0)
                {
                    foreach (IMyTextPanel screen in components2Screen)
                    {
                        if (screen.GetText() != contentComp2)
                        {
                            String[] lines = screen.GetText().Split('\n');

                            if (lines.Length == 11)
                            {
                                int parseResult;
                                pro.parseNumber(lines[3].Split('/')[1], out parseResult);
                                componentsQ["Thrust"] = parseResult;
                                pro.parseNumber(lines[4].Split('/')[1], out parseResult);
                                componentsQ["Superconductor"] = parseResult;
                                pro.parseNumber(lines[5].Split('/')[1], out parseResult);
                                componentsQ["RadioCommunication"] = parseResult;
                                pro.parseNumber(lines[6].Split('/')[1], out parseResult);
                                componentsQ["Detector"] = parseResult;
                                pro.parseNumber(lines[7].Split('/')[1], out parseResult);
                                componentsQ["Medical"] = parseResult;
                                pro.parseNumber(lines[8].Split('/')[1], out parseResult);
                                componentsQ["GravityGenerator"] = parseResult;
                                pro.parseNumber(lines[9].Split('/')[1], out parseResult);
                                componentsQ["Explosives"] = parseResult;
                                pro.parseNumber(lines[10].Split('/')[1], out parseResult);
                                componentsQ["Canvas"] = parseResult;
                            }
                        }
                    }

                    //printing new content
                    contentComp2 = "\n        Components2 quota\n";
                    addLine(ref contentComp2, "Thruster              ", components, componentsQ, "Thrust");
                    addLine(ref contentComp2, "Superconductor        ", components, componentsQ, "Superconductor");
                    addLine(ref contentComp2, "Radio-comm            ", components, componentsQ, "RadioCommunication");
                    addLine(ref contentComp2, "Detector              ", components, componentsQ, "Detector");
                    addLine(ref contentComp2, "Medical               ", components, componentsQ, "Medical");
                    addLine(ref contentComp2, "GravGen               ", components, componentsQ, "GravityGenerator");
                    addLine(ref contentComp2, "Explosives            ", components, componentsQ, "Explosives");
                    addLine(ref contentComp2, "Canvas                ", components, componentsQ, "Canvas");
                    drawStringOnScreens(components2Screen, contentComp2);
                }
            }

            public void drawToolsQuota()
            {
                if (toolsScreen.Count != 0)
                {
                    foreach (IMyTextPanel screen in toolsScreen)
                    {
                        if (screen.GetText() != contentTools)
                        {
                            String[] lines = screen.GetText().Split('\n');

                            int parseResult;

                            if (lines.Length == 15)
                            {
                                pro.parseNumber(lines[3].Split('/')[1], out parseResult);
                                restQ["WelderItem"] = parseResult;
                                pro.parseNumber(lines[4].Split('/')[1], out parseResult);
                                restQ["Welder2Item"] = parseResult;
                                pro.parseNumber(lines[5].Split('/')[1], out parseResult);
                                restQ["Welder3Item"] = parseResult;
                                pro.parseNumber(lines[6].Split('/')[1], out parseResult);
                                restQ["Welder4Item"] = parseResult;
                                pro.parseNumber(lines[7].Split('/')[1], out parseResult);
                                restQ["AngleGrinderItem"] = parseResult;
                                pro.parseNumber(lines[8].Split('/')[1], out parseResult);
                                restQ["AngleGrinder2Item"] = parseResult;
                                pro.parseNumber(lines[9].Split('/')[1], out parseResult);
                                restQ["AngleGrinder3Item"] = parseResult;
                                pro.parseNumber(lines[10].Split('/')[1], out parseResult);
                                restQ["AngleGrinder4Item"] = parseResult;
                                pro.parseNumber(lines[11].Split('/')[1], out parseResult);
                                restQ["HandDrillItem"] = parseResult;
                                pro.parseNumber(lines[12].Split('/')[1], out parseResult);
                                restQ["HandDrill2Item"] = parseResult;
                                pro.parseNumber(lines[13].Split('/')[1], out parseResult);
                                restQ["HandDrill3Item"] = parseResult;
                                pro.parseNumber(lines[14].Split('/')[1], out parseResult);
                                restQ["HandDrill4Item"] = parseResult;
                            }
                        }
                    }

                    //printing new content
                    contentTools = "\n        Tools quota\n";
                    addLine(ref contentTools, "Welder                ", rest, restQ, "WelderItem");
                    addLine(ref contentTools, "Welder *              ", rest, restQ, "Welder2Item");
                    addLine(ref contentTools, "Welder **             ", rest, restQ, "Welder3Item");
                    addLine(ref contentTools, "Welder ***            ", rest, restQ, "Welder4Item");
                    addLine(ref contentTools, "Grinder               ", rest, restQ, "AngleGrinderItem");
                    addLine(ref contentTools, "Grinder *             ", rest, restQ, "AngleGrinder2Item");
                    addLine(ref contentTools, "Grinder **            ", rest, restQ, "AngleGrinder3Item");
                    addLine(ref contentTools, "Grinder ***           ", rest, restQ, "AngleGrinder4Item");
                    addLine(ref contentTools, "Driller               ", rest, restQ, "HandDrillItem");
                    addLine(ref contentTools, "Driller *             ", rest, restQ, "HandDrill2Item");
                    addLine(ref contentTools, "Driller **            ", rest, restQ, "HandDrill3Item");
                    addLine(ref contentTools, "Driller ***           ", rest, restQ, "HandDrill4Item");

                    drawStringOnScreens(toolsScreen, contentTools);
                }
            }

            public void drawRestQuota()
            {
                //TODO this
            }

            public void drawIndustryStatusScreen()
            {
                if (industryScreen.Count != 0)
                {
                    String content = "\n          Industry Overview" +
                        "\n\n             Job             Doing" +
                        "\nRef:      " + (refHelp ? "Help Ass." : "Prio List") + " | " + refHelpText +
                        "\n\nAssemblers: " + assWorkText;
                    drawStringOnScreens(industryScreen, content);
                }
            }

            private void drawStringOnScreens(List<IMyTextPanel> screens, String content)
            {
                foreach (IMyTextPanel screen in screens)
                {
                    screen.WritePublicText(content);
                }
            }

            private void addLine(ref String result, String name, int current, int quota)
            {
                //TODO: make this fill the gap between the name and the IS number according to space left
                result += "\n" + pro.loadingBar((double)current / quota) + " " + name + pro.formatNumber(current) + " / " + pro.formatNumber(quota);
            }

            private void addLine(ref String result, String name, Dictionary<String, float> dic, Dictionary<String, float> dicQ, String key)
            {
                float value;
                float quota;
                addLine(ref result, name, dic.TryGetValue(key, out value) ? (int)value : 0, dicQ.TryGetValue(key, out quota) ? (int)quota : 50000);
            }

        }

        class SolarAdjustment : Extension
        {


            public SolarAdjustment(Program pro) : base(pro)
            {

            }
        }

        struct difference
        {
            public String key;
            public float diff;
            public float ratio;

            public difference(String key, float diff, float ratio)
            {
                this.key = key;
                this.diff = diff;
                this.ratio = ratio;
            }

            public String ToString()
            {
                return "k: " + key + " d: " + diff + " r: " + ratio;
            }
        }
    }
}
