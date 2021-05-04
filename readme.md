# globalazure.at BSCC

All in all, not the cleanest code I've ever written, but it works.
Most of the strategy logic ist in [BattlefieldGenerator.cs](Codeworx.Battleship.Player/BattlefieldGenerator.cs).
The code evolved over time and some of the commented sections are useless in the end but still show the path of the evolution.
## First attempt – Random Simulation
Method SimulateRandom() -> randomly pick possible ship placement combinations and rank the cells by overall density.
Pro:
Good results if you can simulate a large number of combinations.
Con:
Slow and due to the large number of possible combinations an bad results if the simulation set is too small. 
Results were mostly in the 40 – 41 avg. Shots per 500 games.

## Second attempt - Probability density approach
Method CalculcateDensity() -> place ever remaining ship on every possible location and rank the cells by how often a ship was placed there.
Pro:
Mostly good results, fast
Con:
Still huge variation in results (37.8 - 41).
## Third attempt combine Probability with simulation
Use Probability for the first three ships and simulation for the last two.
Pro:
Good results on a more consistent basis.
Con:
A little slower, results ranging from 37.5 – 39.5, 37.5 being more of a lucky punch than a consistently repeatable number.

## Final attempt
After collecting a lot of data from completed games I noticed that the random distribution of the ships has a tendency to create a pattern within a certain period. One time I notices ships tending more to the left side, a couple of hours later more to the to, and so on. So I collected sample date for a little more than 350000 random consecutive games and stored the data. BattleshipController.cs -> Finished().
Then I tried calculating the density based on the previously played games -> SimulateByHistoricData(). It produced very good and consistent results in the low 38 range but was too slow for 500 concurrent games.
So I wrote a little “Compiler” -> Codeworx.Battleship.Generator that was able to compile a decision tree for the collected data. 
The collected data sample was large enough to provide very repeatable results for the first 10 shots. The following shots were calculated by the density function.
Pro:
If the collected data matches the current “random” pattern you get regularly below 37.5, the lowest number I’ve seen locally was 37.17. Otherwise the results were still extremely consistent between 37.8 and 38.5
Con:
You need multiple datasets, one for each “random” pattern and you need to try multiple compiled decision trees to see which one is currently the best one.
