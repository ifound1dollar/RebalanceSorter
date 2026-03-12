# RebalanceSorter

This project implements a type of greedy descending algorithm that balances a given number of units containing a positive integer across a given number of stations (bins). The algorithm's goal is to distribute these units across the bins in a way that keeps the total sum of the values in the bins as near to each other as possible. Additionally, each bin by designed has a limited number of slots available that each unit will take up; a unit with a value <=16 takes up one slot, while a unit with a value >16 takes up two. The user is prompted to input all data when the application is run.

### Usage

Currently, the application receives console input that must be manually entered by the user. The user must enter the number of units with an associated unit identifier and value for each, then must enter the number of stations and the station identifiers and available screens in the same way. It performs basic input sanitation like ensuring that the number of units, the unit values, and each station's available screens are all positive integers. It also ensures that the unit and station identifiers are unique in order to prevent identifier conflicts. Additionally, the user can optionally lock/reserve certain units to specific stations if they desire. 

The application can also be run without input at all, in which case it generates random station and unit data and runs the same algorithm. Regardless of whether data is input manually or randomly generated, the application outputs the results in easy-to-read format.

### Strategy

The algorithm implemented is relatively simple. But before we can implement the algorithm, we must calculate the total number of slots needed for all units and the total number of slots we have available for the bins. For example, if we have a total of 7 bins with five of them having 5 slots available and two having 4 available, our total number of available slots is 33. The number of slots required must be less than or equal to the number of slots available (of course). The algorithm's goal will be to balance every bin with a total sum of values equal to the average of the unit total and the number of bins available (ex. 200 total / 7 stations = ~28.57 per station).

As for the implementation, we take our array (List) and first sort it in descending order so we have our largest units at the start of the array, which often will require 2 slots. Our strategy is to iterate over the entire array of units and add the current (largest) unit to the bin that currently has the smallest total sum of values. Each iteration over the descending-ordered units array, we re-sort the bins in ascending order to ensure that the smallest bin appears first in the array. We find the first bin available that has room for our current unit (1 or 2 slots available depending on the unit size); we then add the unit to the bin. This process continues until we have added every single unit to a bin.

Ultimately, this algorithm converges at a rough average because units are sorted descending while stations are ordered ascending. Because it is simple, it is not 100% optimal and will encounter some issues when the slots available to the stations (bins) vary significantly, like some bins only having 2 slots available in the first place. However, the real-world environment in which this application was built for is such that a bin having less than 4 slots available is exceedingly unlikely. 


<img width="1108" height="773" alt="Sorting Algo" src="https://github.com/user-attachments/assets/e042a83a-770c-4a4e-ade3-41583ee3e2e2" />

