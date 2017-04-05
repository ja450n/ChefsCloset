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
		private Vector2 kitchenRange = new Vector2(6, 0);
		private List<Item> _fridgeItems;
		private List<List<Item>> _chestItems = new List<List<Item>>();

		private void ResolveCookedItems(object seneder, EventArgsClickableMenuClosed e) {
			if (farmHouse != null && e.PriorMenu is CraftingPage && _chestItems.Any()) {
				// remove all used items from fridge and reset fridge inventory
				_fridgeItems.RemoveAll(x => x.Stack == 0);
				farmHouse.fridge.items = _fridgeItems;

				// remove all used items from chests
				foreach (var obj in farmHouse.objects)
				{
					Chest chest = obj.Value as Chest;
					if (chest == null || chest == farmHouse.fridge || obj.Key.X > kitchenRange.X || obj.Key.Y < kitchenRange.Y)
						continue;

					chest.items = _chestItems.First(x => x == chest.items);
					chest.items.RemoveAll(x => x.Stack == 0);
				}

				_chestItems.Clear();
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
					Chest chest = obj.Value as Chest;
					if (chest == null || chest == farmHouse.fridge)
						continue;
					if (obj.Key.X > kitchenRange.X || obj.Key.Y < kitchenRange.Y)
					{
						Monitor.Log($"Chest found out of range at {obj.Key.X},{obj.Key.Y}");
						continue;
					}

					chestKeys.Add(obj.Key);
				}

				// order keys to ensure chest items are consumed in the correct order: left-right/top-bottom
				chestKeys = chestKeys.OrderBy(x => x.X).ToList().OrderBy(x => x.Y).ToList();
				chestKeys.Reverse();

				// consolidate cooking items
				foreach (var chestKey in chestKeys)
				{
					Object chest;
					farmHouse.objects.TryGetValue(chestKey, out chest);

					if (chest != null) {
						Monitor.Log($"Adding {((Chest)chest).items.Count} items from chest at {chestKey.X},{chestKey.Y}");
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
				if (farmHouse.upgradeLevel >= 2)
				{
					kitchenRange.X = 9;
					kitchenRange.Y = 14;
				}
			}
			else {
				farmHouse = null;
			}
		}
	}
}
