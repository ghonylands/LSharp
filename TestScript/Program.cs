using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using Color = System.Drawing.Color;

internal class TestScript
{
    // declare shorthandle to access the player object
    // Properties http://msdn.microsoft.com/en-us/library/aa288470%28v=vs.71%29.aspx 
    private static Obj_AI_Hero Player { get { return ObjectManager.Player; } }

    // declare orbwalker class
    private static Orbwalking.Orbwalker Orbwalker;

    // declare  list of spells
    private static Spell Q, W, E, R;

    // declare list of items
    private static Items.Item Dfg;

    // declare menu
    private static Menu Menu;

    // declare Laugh
    private static int LastLaugh;

    /// <summary>
    /// Default programm entrypoint, gets called once on programm creation
    /// </summary>
    static void Main(string[] args)
    {
        // Events http://msdn.microsoft.com/en-us/library/edzehd2t%28v=vs.110%29.aspx
        // OnGameLoad event, gets fired after loading screen is over
        CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
    }

    /// <summary>
    /// Game Loaded Method
    /// </summary>
    private static void Game_OnGameLoad(EventArgs args)
    {
        if (Player.ChampionName != "Nunu") // check if the current champion is Nunu
            return; // stop programm

        // the Spell class provides methods to check and cast Spells
        // Constructor Spell(SpellSlot slot, float range)
        Q = new Spell(SpellSlot.Q, 125); // create Q spell with a range of 125 units
        W = new Spell(SpellSlot.W, 700); // create W spell with a range of 700 units
        E = new Spell(SpellSlot.E, 550); // create E spell with a range of 550 units
        R = new Spell(SpellSlot.R, 650); // create R spell with a range of 650 units

        // set spells prediction values, not used on Nunu
        // Method Spell.SetSkillshot(float delay, float width, float speed, bool collision, SkillshotType type)
        // Q.SetSkillshot(0.25f, 80f, 1800f, false, SkillshotType.SkillshotLine);

        // create Dfg item id 3128 and range of 750 units
        // Constructor Items.Item(int id, float range)
        Dfg = new Items.Item(3128, 750); // or use ItemId enum
        Dfg = new Items.Item((int)ItemId.Deathfire_Grasp, 750);

        // create root menu
        // Constructor Menu(string displayName, string name, bool root)
        Menu = new Menu(Player.ChampionName, Player.ChampionName, true);

        // create and add submenu 'Orbwalker'
        // Menu.AddSubMenu(Menu menu) returns added Menu
        Menu orbwalkerMenu = Menu.AddSubMenu(new Menu("Orbwalker", "Orbwalker"));

        // creates Orbwalker object and attach to orbwalkerMenu
        // Constructor Orbwalking.Orbwalker(Menu menu);
        Orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);

        // create submenu for TargetSelector used by Orbwalker
        Menu ts = Menu.AddSubMenu(new Menu("Target Selector", "Target Selector")); ;

        // attach
        TargetSelector.AddToMenu(ts);

        //Spells menu
        Menu spellMenu = Menu.AddSubMenu(new Menu("Spells", "Spells"));

        // Menu.AddItem(MenuItem item) returns added MenuItem
        // Constructor MenuItem(string name, string displayName)
        // .SetValue(true) on/off button
        spellMenu.AddItem(new MenuItem("useQ", "Use Q").SetValue(true));
        spellMenu.AddItem(new MenuItem("useW", "Use W").SetValue(true));
        spellMenu.AddItem(new MenuItem("useE", "Use E").SetValue(true));
        spellMenu.AddItem(new MenuItem("useR", "Use R to Farm").SetValue(true));

        // create MenuItem 'LaughButton' as Keybind
        // Constructor KeyBind(int keyCode, KeyBindType type)
        spellMenu.AddItem(new MenuItem("LaughButton", "Combo").SetValue(new KeyBind(32, KeyBindType.Press)));

        // create MenuItem 'ConsumeHealth' as Slider
        // Constructor Slider(int value, int min, int max)
        spellMenu.AddItem(new MenuItem("ConsumeHealth", "Consume below HP").SetValue(new Slider(40, 1, 100)));

        // attach to 'Sift/F9' Menu
        Menu.AddToMainMenu();

        // subscribe to Drawing event
        Drawing.OnDraw += Drawing_OnDraw;

        // subscribe to Update event gets called every game update around 10ms
        Game.OnGameUpdate += Game_OnGameUpdate;

        // print text in local chat
        Game.PrintChat("Welcome to Education Nunu");
    }

    /// <summary>
    /// Main Update Method
    /// </summary>
    private static void Game_OnGameUpdate(EventArgs args)
    {
        // dont do stuff while dead
        if (Player.IsDead)
            return;

        // checks the current Orbwalker mode Combo/Mixed/LaneClear/LastHit
        if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
        {
            // combo to kill the enemy
            Consume();
            Bloodboil();
            Iceblast();
        }

        if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
        {
            // farm and harass
            Consume();
            Iceblast();
        }

        if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
        {
            // fast minion farming
            AbsoluteZero();
        }

        // special keybind pressed (32 = Space)
        if (Menu.Item("LaughButton").GetValue<KeyBind>().Active)
        {
            // send Laugh every 4.20 seconds
            if (Environment.TickCount > LastLaugh + 4200)
            {
                // create Laugh emote packet and send it
                // disabled because packets broken with 4.21
                // Packet.C2S.Emote.Encoded(new Packet.C2S.Emote.Struct((byte)Packet.Emotes.Laugh)).Send();

                // save last time Laugh was send
                LastLaugh = Environment.TickCount;
            }
        }
    }

    /// <summary>
    /// Main Draw Method
    /// </summary>
    private static void Drawing_OnDraw(EventArgs args)
    {
        // dont draw stuff while dead
        if (Player.IsDead)
            return;

        // check if E ready
        if (E.IsReady())
        {
            // draw Aqua circle around the player
            Utility.DrawCircle(Player.Position, Q.Range, Color.Aqua);
        }
        else
        {
            // draw DarkRed circle around the player while on cd
            Utility.DrawCircle(Player.Position, Q.Range, Color.DarkRed);
        }
    }

    /// <summary>
    /// Consume logic
    /// </summary>
    private static void Consume()
    {
        // check if the player wants to use Q
        if (!Menu.Item("useQ").GetValue<bool>())
            return;

        // check if Q ready
        if (Q.IsReady())
        {
            // get sliders value of 'ConsumeHealth'
            int sliderValue = Menu.Item("ConsumeHealth").GetValue<Slider>().Value;

            // calc current percent hp
            float healthPercent = Player.Health / Player.MaxHealth * 100;

            // check if we should heal
            if (healthPercent < sliderValue)
            {
                // get first minion in Q range
                Obj_AI_Base minion = MinionManager.GetMinions(Player.Position, Q.Range).FirstOrDefault();

                // check if we found a minion to consume
                if (minion.IsValidTarget())
                {
                    Q.CastOnUnit(minion); // nom nom nom
                }
            }
        }
    }

    /// <summary>
    /// Bloodboil logic
    /// </summary>
    private static void Bloodboil()
    {
        // check if the player wants to use W
        if (!Menu.Item("useW").GetValue<bool>())
            return;

        // check if W ready
        if (W.IsReady())
        {
            // gets best target in a range of 800 units
            Obj_AI_Hero target = TargetSelector.GetTarget(800, TargetSelector.DamageType.Magical);

            // check if there is an ally in range to buff, be nice :>
            Obj_AI_Hero ally =
                ObjectManager.Get<Obj_AI_Hero>()
                // only get ally + not dead + in W range
                    .Where(hero => hero.IsAlly && !hero.IsDead && Player.Distance(hero) < W.Range)
                // get the ally with the most AttackDamage
                    .OrderByDescending(hero => hero.FlatPhysicalDamageMod).FirstOrDefault();

            // check if we found an ally
            if (ally != null)
            {
                // check if there is a target in our AttackRange or in our ally AttackRange
                if (target.IsValidTarget(Player.AttackRange + 100) || ally.CountEnemysInRange((int)ally.AttackRange + 100) > 0)
                {
                    // buff your ally and yourself
                    W.CastOnUnit(ally);
                }
            }

            // no ally in range to buff, selcast!
            // checks if your target is valid (not dead, not too far away, not in zhonyas etc.)
            // we add +100 to our AttackRange to catch up to the target
            if (target.IsValidTarget(Player.AttackRange + 100))
            {
                // buff yourself
                W.CastOnUnit(Player);
            }
        }
    }

    /// <summary>
    /// Iceblast logic
    /// </summary>
    private static void Iceblast()
    {
        // check if the player wants to use E
        if (!Menu.Item("useE").GetValue<bool>())
            return;

        // gets best target in Dfg(750) / E(550)
        Obj_AI_Hero target = TargetSelector.GetTarget(750, TargetSelector.DamageType.Magical);

        // check if dfg ready
        if (Dfg.IsReady())
        {
            // check if we found a valid target in range
            if (target.IsValidTarget(Dfg.Range))
            {
                // use dfg on him
                Dfg.Cast(target);
            }
        }

        // check if E ready
        if (E.IsReady())
        {
            // check if we found a valid target in range
            if (target.IsValidTarget(E.Range))
            {
                // blast him
                E.CastOnUnit(target);
            }
        }
    }

    private static void AbsoluteZero()
    {
        // check if the player wants to use R
        if (!Menu.Item("useR").GetValue<bool>())
            return;

        // fast lane clear
        // use Nunu R to clear the lane faster
        if (R.IsReady()) // check if R ready
        {
            // get the amount of enemy minions in Ultimate range
            int minionsInUltimateRange = MinionManager.GetMinions(Player.Position, R.Range).Count;

            if (minionsInUltimateRange > 10)
            {
                // cast Ultimate, gold incomming
                R.CastOnUnit(Player);
            }
        }
    }
}