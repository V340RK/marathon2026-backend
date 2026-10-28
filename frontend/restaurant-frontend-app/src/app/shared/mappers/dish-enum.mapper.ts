import { DishSort } from '@app/models/enums/dish-sort.model';
import { DishState } from '@app/models/enums/dish-state.model';
import { DishType } from '@app/models/enums/dish-type.model';

export const DishTypeLabels: Record<DishType, string> = {
  [DishType.Appetizer]: 'Appetizers',
  [DishType.MainCourse]: 'Main Courses',
  [DishType.Dessert]: 'Desserts',
};

export const DishSortingLabels: Record<DishSort, string> = {
  [DishSort.PriceDescending]: 'Price Descending',
  [DishSort.PriceAscending]: 'Price Ascending',
  [DishSort.PopularityDescending]: 'Popularity Descending',
  [DishSort.PopularityAscending]: 'Popularity Ascending',
};

export const DishStateLabels: Record<DishState, string> = {
  [DishState.OnStop]: 'On Stop',
  [DishState.Available]: 'Available',
  [DishState.OutOfStock]: 'Out Of Stock',
};
