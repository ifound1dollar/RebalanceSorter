# RebalanceSorter

This project implements a type of greedy descending algorithm that balances 21 units containing an integer in the range 0-24 across a group of up to 8 stations (bins). The algorithm's goal is to distribute these units across the bins in a way that keeps the total sum of the values in the bins as near to each other as possible. Additionally, each bin has a limited number of slots available (up to 5) that each unit will take up; a unit with a value <=16 takes up one slot, while a unit with a value >16 takes up too. This additional constraint has the potential to cause balancing problems.

### Strategy

The algorithm implemented is relatively simple. But before we can implement the algorithm, we must calculate the total number of slots needed for all 21 units and the total number of slots we have available for the up to 8 bins. For example, if we have a total of 7 bins with five of them having 5 slots available and two having 4 available, our total number of available slots is 33. The number of slots required must be less than or equal to the number of slots available (of course). The algorithm's goal will be to balance every bin with a total sum of values equal to the average of the unit total and the number of bins available (ex. 200 total / 7 stations = ~28.57 per station).

As for the implementation, we take our array (List) and first sort it in descending order so we have our largest units, which often will require 2 slots, at the start of the array. Our strategy is to iterate over the entire array of units and add the current (largest) unit to the bin that currently has the smallest total sum of values. Each iteration over the descending-ordered units array, we re-sort the bins in ascending order to ensure that the smallest bin appears first in the array. We find the first bin available that has room for our current unit (1 or 2 slots available depending on the unit size); we then add the unit to the bin. This process continues until we have added every single unit to a bin.

Ultimately, this algorithm converges at a rough average because units are sorted descending while stations are ordered ascending. Because it is simple, it is not 100% optimal and will encounter some issues when the slots available to the stations (bins) vary significantly, like some bins only having 2 slots available in the first place. However, the real-world environment in which this application was built for is such that a bin having less than 4 slots available is exceedingly unlikely. 

### Usage

Currently, the application only allows simple console input that must be manually entered by the user. It performs basic input sanitation like ensuring unit input includes 21 entries and is in the range 0-24, and station input includes 8 entries and is in the range 0-5. It can also be run without input at all, in which case it generates random station and unit data and runs the same algorithm.

Regardless of whether inputting data manually or using randomly-generated data, the application outputs the results in easy-to-read format.

<img width="1112" height="643" alt="Sorting Algo" src="https://github.com/user-attachments/assets/da22c756-4b78-4ec7-9d45-046d2a90d68d" />
