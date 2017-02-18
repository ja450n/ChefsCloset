using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Locations;
using StardewValley.Objects;

namespace ChefsCloset
{
	public class ModEntry : Mod
	{
		public override void Entry(IModHelper helper)
		{
			LocationEvents.CurrentLocationChanged += UpdateLocation;
			MenuEvents.MenuChanged += ExtendCookingItems;
			MenuEvents.MenuClosed += ResolveCookedItems;
		}

		private FarmHouse farmHouse;
		private List<Item> _fridgeItems;
		private List<List<Item>> _chestItems = new List<List<Item>>();
		private int kitchenRange;

		private void ResolveCookedItems(object seneder, EventArgsClickableMenuClosed e) {
			if (farmHouse != null && e.PriorMenu is CraftingPage) {
				// remove all used items from fridge and reset fridge inventory
				_fridgeItems.RemoveAll(x => x.Stack == 0);
				farmHouse.fridge.items = _fridgeItems;

				// remove all used items from chests
				foreach (var obj in farmHouse.objects)
				{
					if (obj.Value.Name == "Chest" && obj.Key.X <= kitchenRange)
					{
						var chest = (Chest)obj.Value;
						chest.items = _chestItems.First(x => x == chest.items);
						chest.items.RemoveAll(x => x.Stack == 0);
					}
				}
			}
		}

		private void ExtendCookingItems(object sender, EventArgsClickableMenuChanged e)
		{
			// don't proceed unless we're in the farmhouse opening the crafting menu
			if (farmHouse != null && farmHouse.upgradeLevel >= 1 && e.NewMenu is CraftingPage)
			{
				_fridgeItems = farmHouse.fridge.items;
				var cookingItems = new List<Item>();
				var chestKeys = new List<Vector2>();

				// collect chest keys from kitchen tiles
				foreach (var obj in farmHouse.objects) {
					if (obj.Value.Name == "Chest" && obj.Key.X <= kitchenRange)
					{
						chestKeys.Add(obj.Key);
					}
				}

				// order keys to ensure chest items are consumed in the correct order: left-right/top-bottom
				chestKeys.OrderBy(x => x.Y).ToList().OrderBy(x => x.X).ToList();

				// consolidate cooking items
				foreach (var chestKey in chestKeys)
				{
					Object chest;
					farmHouse.objects.TryGetValue(chestKey, out chest);

					if (chest != null) {
						((Chest)chest).items.ForEach(x => Monitor.Log(x.getDescription()));

						_chestItems.Add(((Chest)chest).items);
						cookingItems.AddRange(((Chest)chest).items);
					}
				}
				cookingItems.AddRange(_fridgeItems);

				// apply cooking items
				farmHouse.fridge.items = cookingItems;
			}
		}

		// keeps track of location state
		private void UpdateLocation(object sender, EventArgsCurrentLocationChanged e)
		{
			if (e.NewLocation is FarmHouse)
			{
				farmHouse = (FarmHouse)e.NewLocation;
				kitchenRange = farmHouse.upgradeLevel == 2 ? 9 : 6;
			}
			else {
				farmHouse = null;
			}
		}
	}
}
