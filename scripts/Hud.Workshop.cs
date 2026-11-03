using Godot;

namespace StarfallMiner;

public partial class Hud
{
    private static bool IsWorkshop(string kind) => kind is "systems" or "fabricator" or "fleet" or "market";
    private void WorkshopDone() { if (Game.Paused) OpenPanel("pause"); else ClosePanel(); }
    private ShipSchematic? _shipSchematic;
    private int _recipePage;
    private ScrollContainer? _workshopScroll;
    private readonly int[] _workshopScrollOffsets = new int[4];
    private int _workshopScrollIndex;
    public void RefreshService()
    {
        _shipSchematic?.QueueRedraw();
        if (_equipmentStatus != null) _equipmentStatus.Text = Game.EquipmentMessage;
    }
    private void RebuildWorkshop() => RefreshInventory();

    private void BuildWorkshop(Panel card, float width, float height)
    {
        _workshopCurrency = Label(card, "", new Rect2(22, 62, width - 44, 22), 11, Hologram.Cyan, _mono);
        _workshopMaterials = Label(card, "", new Rect2(22, 87, width - 44, 20), 10, Hologram.Amber, _mono);
        string[] tabs = ["systems", "fabricator", "fleet", "market"];
        string[] names = ["Ship", "Craft", "Fleet", "Market"];
        float tabWidth = (width - 62) / 4;
        for (int i = 0; i < tabs.Length; i++)
        {
            string tab = tabs[i];
            var button = Button(card, "tab_" + tab, names[i], new Rect2(22 + i * (tabWidth + 6), 116, tabWidth, 38), () => OpenPanel(tab), 11);
            HoloButton(button, Hologram.Cyan); button.Selected = PanelKind == tab;
        }
        bool wide = width >= 900;
        float previewHeight = wide ? 280 : height >= 760 ? 160 : 112;
        float bodyTop = wide ? 166 : 178 + previewHeight;
        float bodyLeft = wide ? 346 : 0;
        float bodyHeight = Mathf.Max(48, height - 124 - bodyTop);
        float bodyWidth = width - bodyLeft - (bodyHeight < 326 ? 12 : 0);
        _shipSchematic = new ShipSchematic { Game = Game, Font = _mono, Ink = Hologram,
            Position = new Vector2(22, wide ? 188 : 166), Size = new Vector2(wide ? 300 : width - 44, previewHeight), MouseFilter = MouseFilterEnum.Ignore };
        card.AddChild(_shipSchematic);
        // The fitted hull and footer stay fixed. Only workshop rows scroll, using
        // the same transaction callbacks and button IDs as the existing pages.
        _workshopScrollIndex = PanelKind switch { "fabricator" => 1, "fleet" => 2, "market" => 3, _ => 0 };
        _workshopScroll = new ScrollContainer { Position = new Vector2(bodyLeft, bodyTop),
            Size = new Vector2(width - bodyLeft, bodyHeight),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
            FollowFocus = true, ClipContents = true, MouseFilter = MouseFilterEnum.Stop };
        card.AddChild(_workshopScroll);
        var body = new Control { CustomMinimumSize = new Vector2(bodyWidth, 326), MouseFilter = MouseFilterEnum.Ignore };
        _workshopScroll.AddChild(body);
        switch (PanelKind)
        {
            case "systems": BuildShipSystems(body, bodyWidth); break;
            case "fabricator": BuildFabricator(body, bodyWidth); break;
            case "fleet": BuildFleet(body, bodyWidth); break;
            case "market": BuildMarket(body, bodyWidth); break;
        }
        _workshopScroll.SetDeferred(ScrollContainer.PropertyName.ScrollVertical, _workshopScrollOffsets[_workshopScrollIndex]);
        _equipmentStatus = Label(card, Game.EquipmentMessage, new Rect2(22, height - 118, width - 44, 50), 11, Hologram.Amber);
        _equipmentStatus.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        Button(card, "done", Game.Paused ? "‹ Back to pause" : "Back to mining", new Rect2(22, height - 62, width - 44, 44), WorkshopDone, 13, Hologram.Cyan);
    }

    private void WorkshopRow(Node parent, Rect2 rect)
    {
        var row = Panel(parent, rect);
        row.AddThemeStyleboxOverride("panel", Style(new Color(Hologram.Shadow, 0.12f), new Color(Hologram.Cyan, 0.12f)));
    }

    private void BuildShipSystems(Node card, float width)
    {
        const float y = 0;
        var goal = Label(card, Game.NextObjective, new Rect2(22, y, width - 44, 50), 12, Hologram.Cyan);
        goal.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        Button(card, "hardpoint", Game.Equipment.HardpointLevel == 2 ? "3 hardpoints installed" : $"Install mount {Game.ShipHardpoints + 1} / {Main.Compact(Game.HardpointCost)} CR / R{Game.Equipment.HardpointLevel + 2}",
            new Rect2(22, y + 62, width - 44, 46), () => Game.BuyHardpoint(), 11, Hologram.Cyan);
        Button(card, "open_fleet", "Fit & upgrade weapons  ›", new Rect2(22, y + 120, width - 44, 46), () => { _fleetDrones = false; OpenPanel("fleet"); }, 13, Hologram.Cyan);
        var refinery = Button(card, "upgrade_2", "", new Rect2(22, y + 178, width - 44, 58), () => Game.BuyUpgrade(2));
        Label(refinery, "Ship refinery", new Rect2(12, 4, width - 190, 23), 13, Ink, _mono);
        _upgradeIndices.Add(2);
        _upgradeLevels.Add(Label(refinery, "", new Rect2(12, 28, width - 190, 22), 9, MutedInk, _mono));
        _upgradeCosts.Add(Label(refinery, "", new Rect2(width - 170, 10, 110, 40), 15, Gold, _mono));
        Button(card, "recall_fleet", "Recall all drones for service", new Rect2(22, y + 248, width - 44, 40), Game.RecallFleet, 12, Hologram.Amber);
    }

    private void BuildFabricator(Node card, float width)
    {
        const float y = 0;
        float half = (width - 56) * 0.5f;
        Button(card, "craft_weapons", "Weapons", new Rect2(22, y, half, 34), () => { _fabricateDrones = false; _recipePage = 0; RebuildWorkshop(); }, 12).Selected = !_fabricateDrones;
        Button(card, "craft_drones", "Drones", new Rect2(34 + half, y, half, 34), () => { _fabricateDrones = true; _recipePage = 0; RebuildWorkshop(); }, 12).Selected = _fabricateDrones;
        int count = 0;
        foreach (var recipe in Game.Catalog.Recipes) if (recipe.Drone == _fabricateDrones) count++;
        _recipePage = Math.Clamp(_recipePage, 0, (count - 1) / 3);
        int cursor = 0;
        for (int i = 0; i < Game.Catalog.Recipes.Count; i++)
        {
            var recipe = Game.Catalog.Recipes[i];
            if (recipe.Drone != _fabricateDrones) continue;
            int row = cursor++ - _recipePage * 3;
            if (row < 0 || row >= 3) continue;
            int recipeIndex = i;
            float top = y + 44 + row * 78;
            WorkshopRow(card, new Rect2(14, top - 2, width - 28, 76));
            Label(card, recipe.Title + $" / R{recipe.Rank}", new Rect2(22, top, width - 44, 20), 13, Ink, _mono);
            Label(card, recipe.Description, new Rect2(22, top + 21, width - 44, 18), 10, MutedInk);
            Label(card, Game.RecipeCost(recipe), new Rect2(22, top + 44, width - 144, 24), 9, Hologram.Amber, _mono);
            Button(card, $"craft_{recipeIndex}", Game.EngineeringRank < recipe.Rank ? "LOCKED" : "CRAFT", new Rect2(width - 116, top + 40, 94, 32), () => Game.Craft(recipeIndex), 12);
        }
        if (count > 3)
        {
            Button(card, "recipes_prev", "‹", new Rect2(22, y + 284, 50, 30), () => { _recipePage = Math.Max(0, _recipePage - 1); RebuildWorkshop(); });
            Label(card, $"BLUEPRINTS {_recipePage + 1}/{(count + 2) / 3}", new Rect2(80, y + 284, width - 160, 30), 9, MutedInk, _mono).HorizontalAlignment = HorizontalAlignment.Center;
            Button(card, "recipes_next", "›", new Rect2(width - 72, y + 284, 50, 30), () => { _recipePage++; RebuildWorkshop(); });
        }
    }

    private void BuildFleet(Node card, float width)
    {
        const float body = 0;
        float half = (width - 56) * 0.5f;
        Button(card, "fleet_weapons", "Weapons", new Rect2(22, body, half, 34), () => { _fleetDrones = false; _fleetPage = 0; RebuildWorkshop(); }, 12).Selected = !_fleetDrones;
        Button(card, "fleet_drones", "Drones", new Rect2(34 + half, body, half, 34), () => { _fleetDrones = true; RebuildWorkshop(); }, 12).Selected = _fleetDrones;
        if (_fleetDrones)
        {
            if (Game.Equipment.Drones.Count == 0) Label(card, "No drones yet. Build one in Craft → Drones.", new Rect2(22, body + 44, width - 44, 36), 12, MutedInk);
            for (int i = 0; i < Game.Equipment.Drones.Count; i++)
            {
                var drone = Game.Equipment.Drones[i];
                float y = body + 42 + i * 90;
                WorkshopRow(card, new Rect2(14, y - 2, width - 28, 86));
                Label(card, $"{Game.Catalog.Chassis(drone.Type).Title}  LV {drone.Level}", new Rect2(22, y, width - 144, 20), 13, Ink, _mono);
                var mount = Game.Equipment.FindWeapon(drone.WeaponId);
                string state = drone.Docked ? "DOCKED" : drone.Recalling ? "RETURNING" : "MINING";
                Label(card, state, new Rect2(width - 116, y, 94, 20), 10, Hologram.Amber, _mono);
                Label(card, mount == null ? "UNARMED / fit a reserve weapon" : $"MOUNT / {Main.Weapons[mount.Type]} LV {mount.Level}", new Rect2(22, y + 24, width - 44, 20), 11, MutedInk);
                float third = (width - 60) / 3;
                Button(card, $"fit_{drone.Id}", "Fit / change", new Rect2(22, y + 45, third, 34), () => Game.CycleDroneWeapon(drone.Id), 11);
                Button(card, $"drone_recall_{drone.Id}", drone.Docked ? "Deploy" : "Recall", new Rect2(30 + third, y + 45, third, 34), () => { if (drone.Docked) Game.DeployDrone(drone.Id); else Game.RecallDrone(drone.Id); }, 11);
                Button(card, $"drone_upgrade_{drone.Id}", $"↑ {Main.Compact(Game.DroneUpgradeCost(drone))} CR", new Rect2(38 + third * 2, y + 45, third, 34), () => Game.UpgradeEquipment(drone.Id, true), 9, Hologram.Amber);
            }
            return;
        }
        _fleetPage = Math.Clamp(_fleetPage, 0, (Game.Equipment.Weapons.Count - 1) / 2);
        for (int row = 0; row < 2; row++)
        {
            int index = _fleetPage * 2 + row;
            if (index >= Game.Equipment.Weapons.Count) break;
            var item = Game.Equipment.Weapons[index];
            float y = body + 42 + row * 122;
            WorkshopRow(card, new Rect2(14, y - 2, width - 28, 118));
            Label(card, $"{Main.Weapons[item.Type]}  LV {item.Level}", new Rect2(22, y, width - 44, 21), 13, Ink, _mono);
            string location = Game.Equipment.ShipMounted(item.Id) ? "ON SHIP" : Game.Equipment.Mounted(item.Id) ? "ON DRONE" : "RESERVE";
            Label(card, $"{location} / POWER +{Math.Round((Game.WeaponPower(item.Level) - 1) * 100)}% / RATE +{Math.Round((Main.FireRateBonus(item.RateLevel) - 1) * 100)}%", new Rect2(22, y + 22, width - 44, 16), 9, MutedInk, _mono);
            Button(card, $"equip_{item.Id}", item.Id == Game.Equipment.ShipWeaponId ? "Primary" : Game.Equipment.ShipMounted(item.Id) ? "Unfit aux" : "Fit ship", new Rect2(22, y + 42, half, 30), () => Game.ToggleShipMount(item.Id), 11);
            Button(card, $"primary_{item.Id}", "Set primary", new Rect2(34 + half, y + 42, half, 30), () => Game.EquipShip(item.Id), 11);
            string power = item.Level >= Game.EquipmentLevelCap ? "Power / rank limit" : $"Power ↑ / {Main.Compact(Game.WeaponUpgradeCost(item))} CR";
            string rate = item.RateLevel >= Game.EquipmentLevelCap ? "Rate / rank limit" : $"Fire rate ↑ / {Main.Compact(Game.FireRateUpgradeCost(item))} CR";
            Button(card, $"weapon_upgrade_{item.Id}", power, new Rect2(22, y + 78, half, 36), () => Game.UpgradeEquipment(item.Id, false), 10, Hologram.Amber);
            Button(card, $"rate_upgrade_{item.Id}", rate, new Rect2(34 + half, y + 78, half, 36), () => Game.UpgradeFireRate(item.Id), 10, Hologram.Cyan);
        }
        Button(card, "weapons_prev", "‹", new Rect2(22, body + 294, 52, 30), () => { _fleetPage = Math.Max(0, _fleetPage - 1); RebuildWorkshop(); }, 15);
        Label(card, $"PAGE {_fleetPage + 1} / {(Game.Equipment.Weapons.Count + 1) / 2}", new Rect2(86, body + 294, width - 172, 30), 10, MutedInk, _mono).HorizontalAlignment = HorizontalAlignment.Center;
        Button(card, "weapons_next", "›", new Rect2(width - 74, body + 294, 52, 30), () => { _fleetPage++; RebuildWorkshop(); }, 15);
    }

    private void BuildMarket(Node card, float width)
    {
        Label(card, "Buy rare materials with credits. Fixed prices; no waiting.", new Rect2(22, 0, width - 44, 24), 11, Hologram.Cyan);
        for (int i = 0; i < 3; i++)
        {
            int material = i;
            float y = 34 + i * 85;
            int price = Game.Catalog.MarketPrices[i];
            WorkshopRow(card, new Rect2(14, y - 2, width - 28, 81));
            Label(card, Game.Catalog.MaterialNames[i], new Rect2(22, y, width - 44, 24), 13, Game.Catalog.MaterialColors[i], _mono);
            float third = (width - 60) / 3;
            Button(card, $"buy_{i}", $"Buy 1 / {Main.Compact(price)}", new Rect2(22, y + 34, third, 40), () => Game.TradeMaterial(material, 1, true), 11);
            Button(card, $"buy5_{i}", $"Buy 5 / {Main.Compact(price * 5)}", new Rect2(30 + third, y + 34, third, 40), () => Game.TradeMaterial(material, 5, true), 11);
            Button(card, $"sell_{i}", $"Sell 1 / {Main.Compact(Math.Floor(price * 0.35))}", new Rect2(38 + third * 2, y + 34, third, 40), () => Game.TradeMaterial(material, 1, false), 11, Hologram.Amber);
        }
    }
}
