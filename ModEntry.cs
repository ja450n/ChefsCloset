using System.Collections.Generic;
using System.Linq;
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
				_fridgeItems.RemoveAll(x => x.Stack == 0);
				farmHouse.fridge.items = _fridgeItems;

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
				var cookingItems = new List<Item>(_fridgeItems);

				foreach (var obj in farmHouse.objects)
				{	
					if (obj.Value.Name == "Chest" && obj.Key.X <= kitchenRange) {
						var chest = (Chest)obj.Value;
						_chestItems.Add(chest.items);
						cookingItems.AddRange(chest.items);
					}
				}

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
