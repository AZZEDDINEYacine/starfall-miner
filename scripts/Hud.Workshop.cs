using Godot;

namespace StarfallMiner;

public partial class Hud
{
    private static bool IsWorkshop(string kind) => kind is "systems" or "fabricator" or "fleet" or "market";
    private void WorkshopDone() { if (Game.Paused) OpenPanel("pause"); else ClosePanel(); }
    private ShipSchematic? _shipSchematic;
    private int _recipePage;
    private float _workshopBody;
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
        string[] names = ["SHIP", "CRAFT", "FLEET", "MARKET"];
        float tabWidth = (width - 62) / 4;
        for (int i = 0; i < tabs.Length; i++)
        {
            string tab = tabs[i];
            var button = Button(card, "tab_" + tab, names[i], new Rect2(22 + i * (tabWidth + 6), 116, tabWidth, 38), () => OpenPanel(tab), 11);
            HoloButton(button, Hologram.Cyan); button.Selected = PanelKind == tab;
        }
        float previewHeight = height >= 760 ? 160 : 112;
        _workshopBody = 178 + previewHeight;
        _shipSchematic = new ShipSchematic { Game = Game, Font = _mono, Position = new Vector2(22, 166), Size = new Vector2(width - 44, previewHeight), MouseFilter = MouseFilterEnum.Ignore };
        card.AddChild(_shipSchematic);
        switch (PanelKind)
        {
            case "systems": BuildShipSystems(card, width, height); break;
            case "fabricator": BuildFabricator(card, width); break;
            case "fleet": BuildFleet(card, width); break;
            case "market": BuildMarket(card, width); break;
        }
        _equipmentStatus = Label(card, Game.EquipmentMessage, new Rect2(22, height - 118, width - 44, 50), 11, Hologram.Amber);
        _equipmentStatus.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        Button(card, "done", Game.Paused ? "‹ BACK TO PAUSE" : "BACK TO MINING", new Rect2(22, height - 62, width - 44, 44), WorkshopDone, 12, Hologram.Cyan);
    }

    private void BuildShipSystems(Panel card, float width, float height)
    {
        float y = _workshopBody;
        var goal = Label(card, Game.NextObjective, new Rect2(22, y, width - 44, 50), 12, Hologram.Cyan);
        goal.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        Button(card, "hardpoint", Game.Equipment.HardpointLevel == 2 ? "3 HARDPOINTS INSTALLED" : $"INSTALL MOUNT {Game.ShipHardpoints + 1} / {Main.Compact(Game.HardpointCost)} CR / R{Game.Equipment.HardpointLevel + 2}",
            new Rect2(22, y + 62, width - 44, 46), () => Game.BuyHardpoint(), 11, Hologram.Cyan);
        Button(card, "open_fleet", "FIT & UPGRADE WEAPONS  ›", new Rect2(22, y + 120, width - 44, 46), () => { _fleetDrones = false; OpenPanel("fleet"); }, 12, Hologram.Cyan);
        var refinery = Button(card, "upgrade_2", "", new Rect2(22, y + 178, width - 44, 58), () => Game.BuyUpgrade(2));
        Label(refinery, "SHIP REFINERY", new Rect2(12, 4, width - 190, 23), 12, Ink, _mono);
        _upgradeIndices.Add(2);
        _upgradeLevels.Add(Label(refinery, "", new Rect2(12, 28, width - 190, 22), 9, MutedInk, _mono));
        _upgradeCosts.Add(Label(refinery, "", new Rect2(width - 170, 10, 110, 40), 15, Gold, _mono));
        Button(card, "recall_fleet", "RECALL ALL DRONES FOR SERVICE", new Rect2(22, y + 248, width - 44, 40), Game.RecallFleet, 10, Hologram.Amber);
    }

    private void BuildFabricator(Panel card, float width)
    {
        float y = _workshopBody;
        float half = (width - 56) * 0.5f;
        Button(card, "craft_weapons", "WEAPONS", new Rect2(22, y, half, 34), () => { _fabricateDrones = false; _recipePage = 0; RebuildWorkshop(); }, 12, !_fabricateDrones ? Hologram.Cyan : MutedInk);
        Button(card, "craft_drones", "DRONES", new Rect2(34 + half, y, half, 34), () => { _fabricateDrones = true; _recipePage = 0; RebuildWorkshop(); }, 12, _fabricateDrones ? Hologram.Cyan : MutedInk);
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
            Label(card, recipe.Title.ToUpperInvariant() + $" / R{recipe.Rank}", new Rect2(22, top, width - 44, 20), 12, Hologram.Cyan, _mono);
            Label(card, recipe.Description, new Rect2(22, top + 21, width - 44, 18), 10, MutedInk);
            Label(card, Game.RecipeCost(recipe), new Rect2(22, top + 44, width - 144, 24), 9, Hologram.Amber, _mono);
            Button(card, $"craft_{recipeIndex}", Game.EngineeringRank < recipe.Rank ? "LOCKED" : "CRAFT", new Rect2(width - 116, top + 40, 94, 32), () => Game.Craft(recipeIndex), 11);
        }
        if (count > 3)
        {
            Button(card, "recipes_prev", "‹", new Rect2(22, y + 284, 50, 30), () => { _recipePage = Math.Max(0, _recipePage - 1); RebuildWorkshop(); });
            Label(card, $"BLUEPRINTS {_recipePage + 1}/{(count + 2) / 3}", new Rect2(80, y + 284, width - 160, 30), 9, MutedInk, _mono).HorizontalAlignment = HorizontalAlignment.Center;
            Button(card, "recipes_next", "›", new Rect2(width - 72, y + 284, 50, 30), () => { _recipePage++; RebuildWorkshop(); });
        }
    }

    private void BuildFleet(Panel card, float width)
    {
        float body = _workshopBody;
        float half = (width - 56) * 0.5f;
        Button(card, "fleet_weapons", "WEAPONS", new Rect2(22, body, half, 34), () => { _fleetDrones = false; _fleetPage = 0; RebuildWorkshop(); }, 12, !_fleetDrones ? Hologram.Cyan : MutedInk);
        Button(card, "fleet_drones", "DRONES", new Rect2(34 + half, body, half, 34), () => { _fleetDrones = true; RebuildWorkshop(); }, 12, _fleetDrones ? Hologram.Cyan : MutedInk);
        if (_fleetDrones)
        {
            if (Game.Equipment.Drones.Count == 0) Label(card, "No drones yet. Build one in Craft → Drones.", new Rect2(22, body + 44, width - 44, 36), 12, MutedInk);
            for (int i = 0; i < Game.Equipment.Drones.Count; i++)
            {
                var drone = Game.Equipment.Drones[i];
                float y = body + 42 + i * 90;
                Label(card, $"{Game.Catalog.Chassis(drone.Type).Title.ToUpperInvariant()}  LV {drone.Level}", new Rect2(22, y, width - 44, 20), 12, Hologram.Cyan, _mono);
                var mount = Game.Equipment.FindWeapon(drone.WeaponId);
                string state = drone.Docked ? "DOCKED" : drone.Recalling ? "RETURNING" : "MINING";
                Label(card, state, new Rect2(width - 116, y, 94, 20), 10, Hologram.Amber, _mono);
                Label(card, mount == null ? "UNARMED / fit a reserve weapon" : $"MOUNT / {Main.Weapons[mount.Type]} LV {mount.Level}", new Rect2(22, y + 24, width - 44, 20), 11, MutedInk);
                float third = (width - 60) / 3;
                Button(card, $"fit_{drone.Id}", "FIT / CHANGE", new Rect2(22, y + 45, third, 34), () => Game.CycleDroneWeapon(drone.Id), 9);
                Button(card, $"drone_recall_{drone.Id}", drone.Docked ? "DEPLOY" : "RECALL", new Rect2(30 + third, y + 45, third, 34), () => { if (drone.Docked) Game.DeployDrone(drone.Id); else Game.RecallDrone(drone.Id); }, 9);
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
            Label(card, $"{Main.Weapons[item.Type].ToUpperInvariant()}  LV {item.Level}", new Rect2(22, y, width - 44, 21), 12, Hologram.Cyan, _mono);
            string location = Game.Equipment.ShipMounted(item.Id) ? "ON SHIP" : Game.Equipment.Mounted(item.Id) ? "ON DRONE" : "RESERVE";
            Label(card, $"{location} / POWER +{Math.Round((Game.WeaponPower(item.Level) - 1) * 100)}% / RATE +{Math.Round((Main.FireRateBonus(item.RateLevel) - 1) * 100)}%", new Rect2(22, y + 22, width - 44, 16), 9, MutedInk, _mono);
            Button(card, $"equip_{item.Id}", item.Id == Game.Equipment.ShipWeaponId ? "PRIMARY" : Game.Equipment.ShipMounted(item.Id) ? "UNFIT AUX" : "FIT SHIP", new Rect2(22, y + 42, half, 30), () => Game.ToggleShipMount(item.Id), 10);
            Button(card, $"primary_{item.Id}", "SET PRIMARY", new Rect2(34 + half, y + 42, half, 30), () => Game.EquipShip(item.Id), 10);
            string power = item.Level >= Game.EquipmentLevelCap ? "POWER / RANK LIMIT" : $"POWER ↑ / {Main.Compact(Game.WeaponUpgradeCost(item))} CR";
            string rate = item.RateLevel >= Game.EquipmentLevelCap ? "RATE / RANK LIMIT" : $"FIRE RATE ↑ / {Main.Compact(Game.FireRateUpgradeCost(item))} CR";
            Button(card, $"weapon_upgrade_{item.Id}", power, new Rect2(22, y + 78, half, 36), () => Game.UpgradeEquipment(item.Id, false), 10, Hologram.Amber);
            Button(card, $"rate_upgrade_{item.Id}", rate, new Rect2(34 + half, y + 78, half, 36), () => Game.UpgradeFireRate(item.Id), 10, Hologram.Cyan);
        }
        Button(card, "weapons_prev", "‹", new Rect2(22, body + 294, 52, 30), () => { _fleetPage = Math.Max(0, _fleetPage - 1); RebuildWorkshop(); }, 15);
        Label(card, $"PAGE {_fleetPage + 1} / {(Game.Equipment.Weapons.Count + 1) / 2}", new Rect2(86, body + 294, width - 172, 30), 10, MutedInk, _mono).HorizontalAlignment = HorizontalAlignment.Center;
        Button(card, "weapons_next", "›", new Rect2(width - 74, body + 294, 52, 30), () => { _fleetPage++; RebuildWorkshop(); }, 15);
    }

    private void BuildMarket(Panel card, float width)
    {
        Label(card, "Buy rare materials with credits. Fixed prices; no waiting.", new Rect2(22, _workshopBody, width - 44, 24), 11, Hologram.Cyan);
        for (int i = 0; i < 3; i++)
        {
            int material = i;
            float y = _workshopBody + 34 + i * 85;
            int price = Game.Catalog.MarketPrices[i];
            Label(card, Game.Catalog.MaterialNames[i].ToUpperInvariant(), new Rect2(22, y, width - 44, 24), 13, Game.Catalog.MaterialColors[i], _mono);
            float third = (width - 60) / 3;
            Button(card, $"buy_{i}", $"BUY 1 / {Main.Compact(price)}", new Rect2(22, y + 34, third, 40), () => Game.TradeMaterial(material, 1, true), 10);
            Button(card, $"buy5_{i}", $"BUY 5 / {Main.Compact(price * 5)}", new Rect2(30 + third, y + 34, third, 40), () => Game.TradeMaterial(material, 5, true), 10);
            Button(card, $"sell_{i}", $"SELL 1 / {Main.Compact(Math.Floor(price * 0.35))}", new Rect2(38 + third * 2, y + 34, third, 40), () => Game.TradeMaterial(material, 1, false), 10, Hologram.Amber);
        }
    }
}
