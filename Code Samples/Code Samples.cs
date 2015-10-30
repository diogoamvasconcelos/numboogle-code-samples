//Class that defines a solution composed of integer values. This solutions are used to fill the cells of the levels
public class IntegerSolution : List<int>
{
    public IntegerSolution() : base()
    { }

    public IntegerSolution(List<int> list) : base(list)
    { }

    public IntegerSolution(Solution solution)
    {
        foreach (CellBase cell in solution)
            this.Add(cell.Value);

        this.Sort();
    }

    public override bool Equals(object obj)
    {
        IntegerSolution otherSolution = (IntegerSolution)obj;

        if (otherSolution == null || this.Count != otherSolution.Count)
            return false;

        return this.SequenceEqual(otherSolution);
    }
    
    //In order to compare solutions, the order of the elements should be neglected but because the value 
    //are not sorted in the list, the same values on different order will not be equal.
    //By sorting a copy of the values before calculating the hash, the correct comparison of two solutions is possible
    //without actually sorting the values (not desired).
    public override int GetHashCode()
    {
        //Copy the values and sort them
        var sortedList = this.ToArray();
        Array.Sort(sortedList);

        //combine hashes
        //source http://stackoverflow.com/questions/1646807/quick-and-simple-hash-code-combinations
        int hash = 17;

        foreach (int number in sortedList)
        {
            hash = hash * 31 + number.GetHashCode();
        }

        return hash;
    }
}

//This function uses recursive backtracking to try to place a list of solutions on the grid, trying to make use of the 
//least amount of cells (usings already assigned cells if possible) while trying to avoid creating addtional solutions.

static bool PlaceSolutionsInGrid(CellBase[,] grid, List<IntegerSolution> orderedSolutions, IntegerSolution origSolution, int target, Main.SolvingDelegate solvDelegate, CellBase previousCell)
{
    if (orderedSolutions.Count == 0)
    {
        //All solutions successfully placed
        return true;
    }

    var solution = orderedSolutions.First();

    if (solution.Count == 0)
    {
        orderedSolutions.Remove(solution);

        if(orderedSolutions.Count == 0)
        {
            //All solutions successfully placed
            return true;
        }
        else
        {
            origSolution = new IntegerSolution(orderedSolutions.First());
             //all numbers successfully placed, go to next solution;
            return PlaceSolutionsInGrid(grid, orderedSolutions, origSolution, target, solvDelegate, null);
        }
    }

    var number = solution.First();
    solution.Remove(number);

    CellBase cell = null;
    //If its the first value of a solution, this value can be placed in any cell of the grid.
    //If not, the value must be placed in the neighbouring cells of the previous placed value
    bool firstValue = previousCell == null;

    //Randomly shuffle the index list, so the grid is iterated in a random order
    //If not the first value (previous cell exists), then just iterate through its neighbouring cells
    var gridCellsIndex = new List<int>(Enumerable.Range(0, (firstValue) ? grid.Length : 9));
    gridCellsIndex.Shuffle();
    
    //Search if is there already a cell with this value
    foreach (var cellIndex in gridCellsIndex)
    {
        int indexX = (firstValue) ? (cellIndex % grid.GetLength(0)) : (previousCell.Index.x + cellIndex % 3 - 1);
        int indexY = (firstValue) ? (cellIndex / grid.GetLength(0)) : (previousCell.Index.y + cellIndex / 3 - 1);

        //Dont use the same cell
        if (!firstValue && indexX == previousCell.Index.x && indexY == previousCell.Index.y) 
            continue;

        //Index check, could be out of bounds
        if (indexX < 0 || indexX >= grid.GetLength(0) || indexY < 0 || indexY >= grid.GetLength(1))
            continue;

        cell = grid[indexX, indexY];

        if (cell.Value == number)
        {
            //recur to see if it is successfully
            if (PlaceSolutionsInGrid(grid, orderedSolutions, origSolution, target, solvDelegate, cell)) 
                return true;
        }
    }

    //Cell with the value approach did not work (did not exist, or no successful solution was found)
    //Try out the rest of the cells that are unnassigned
    foreach (var cellIndex in gridCellsIndex)
    {
        int indexX = (firstValue) ? (cellIndex % grid.GetLength(0)) : (previousCell.Index.x + cellIndex % 3 - 1);
        int indexY = (firstValue) ? (cellIndex / grid.GetLength(0)) : (previousCell.Index.y + cellIndex / 3 - 1);

        //Dont use the same cell
        if (!firstValue && indexX == previousCell.Index.x && indexY == previousCell.Index.y)
            continue;

        //index check
        if (indexX < 0 || indexX >= grid.GetLength(0) || indexY < 0 || indexY >= grid.GetLength(1))
            continue;

        cell = grid[indexX, indexY];

        if (cell.Value == UNASSIGNED && NumberCanBeAssigned(grid, cell, number, target, origSolution, solvDelegate))
        {
            cell.Value = number; //try assigning the number
           
            //recur to see if it is successfully
            if (PlaceSolutionsInGrid(grid, orderedSolutions, origSolution ,target, solvDelegate, cell)) 
                return true;

            cell.Value = UNASSIGNED; //unassign the number to try a new one
        }
    }

    solution.Insert(0, number); //Revert this number selection, placing the number as the first and continue testing

    if(solution.Count == origSolution.Count)
    {
        orderedSolutions.Insert(0, new IntegerSolution()); //Revert to previous solution
    }

    if (IsGridAllAssigned(grid))
    {
        Debug.Log("Couldnt place all solutions because there was no more space left");
        return true; //all cells successfully assigned, but not all solutions were placed (no space)...
    }
    else
    {
        return false; //triggers backtracting to previous/early choice
    }
}

//Tests where a number can be assigned in a cell without creating a new undesired solution
static bool NumberCanBeAssigned(CellBase[,] grid, CellBase cell, int number, int target, 
                                IntegerSolution solution, Main.SolvingDelegate solvDelegate)
{
    CellBase cellToCheck;

    for (int i = -1; i <= 1; ++i)
    {
        for (int j = -1; j <= 1; ++j)
        {
            if (i == 0 && j == 0)
                continue;

            int indexX = cell.Index.x + i;
            int indexY = cell.Index.y + j;

            if(indexX < 0 || indexX >= grid.GetLength(0) || indexY < 0 || indexY >= grid.GetLength(1))
                continue;

            cellToCheck = grid[indexX, indexY];

            if (cellToCheck.Value != UNASSIGNED && !solution.Contains(cellToCheck.Value) &&
                solvDelegate(number, cellToCheck.Value) == target)
                return false;
        }
    }

    return true;
}

//Fill a grid with a certain list of possible numbers, while trying to avoid creating additional solutions
public static bool FillGrid(CellBase[,] grid, int target, List<int> availableNumbers, Main.SolvingDelegate solvDelegate)
{
    CellBase cell = null;

    if (!FindUnassignedCell(grid, ref cell))
        return true; //all cells successfully assigned;

    var shuffledNumbers = new List<int>(availableNumbers);
    shuffledNumbers.Shuffle();

    var emptySolution = new IntegerSolution(); //empty solution to be passed in NumberCanBeAssigned method 

    foreach (var number in shuffledNumbers)
    {
        if (NumberCanBeAssigned(grid, cell, number, target, emptySolution, solvDelegate))
        {
            cell.Value = number; //try assign

            if (FillGrid(grid, target, availableNumbers, solvDelegate)) //recur to see if it is successfully
                return true;

            cell.Value = UNASSIGNED; //undo assign
        }
    }

    return false;
}

//Finds a cell in the grid that is currently unassigned
static bool FindUnassignedCell(CellBase[,] grid, ref CellBase unassignedCell)
{
    foreach (CellBase cell in grid)
    {
        if (cell.Value == UNASSIGNED)
        {
            unassignedCell = cell;
            return true;
        }
    }

    return false;
}

//This class defines a solution, that is a set of cells from the grid, whose values form a solution
//.NET 3.5 which Unity supports does not have SortedSet collection (tree based, sorted).
//Use Hashset instead (Hash table based, random order)
public class Solution : HashSet<CellBase>
{
    public override bool Equals(object obj)
    {
        Solution otherSolution = (Solution)obj;

        if (otherSolution == null || this.Count != otherSolution.Count)
            return false;

        return this.SetEquals(otherSolution);
    }

    //In order to compare solutions, the set must be the same. Because a Hashset structure is being used,3
    //the value are not sorted so when the hash is calculated (from comparisson purposes for example) 
    //the set is coppied to a list, then sorted by the index of the cells in the solution.
    //This enables the correct comparison of two solutions, because the order is neglected (as they are both sorted)
    public override int GetHashCode()
    {
        //sort cells. Cells are sorted by their index (which must be unique, unsuring a deterministic sorting)
        var sortedList = this.ToArray();
        Array.Sort(sortedList, FunctionalComparer<CellBase>.Create((CellBase cellA, CellBase cellB) =>
                                                                    (cellA.Index.x * 1000 + cellA.Index.y > cellB.Index.x * 1000 + cellB.Index.y) ?
                                                                    1 :
                                                                    (cellA.Index.x * 1000 + cellA.Index.y < cellB.Index.x * 1000 + cellB.Index.y) ? -1 : 0));

        //combine hashes
        //source http://stackoverflow.com/questions/1646807/quick-and-simple-hash-code-combinations
        int hash = 17;

        foreach (CellBase cell in sortedList)
        {
            hash = hash * 31 + cell.GetHashCode();
        }

        return hash;
    }
}

//Equivalent to .NET 4.5 Comparer<T> 
//source: http://stackoverflow.com/questions/16839479/using-lambda-expression-in-place-of-icomparer-argument
public class FunctionalComparer<T> : IComparer<T>
{
    private Func<T, T, int> comparer;
    public FunctionalComparer(Func<T, T, int> comparer)
    {
        this.comparer = comparer;
    }
    public static IComparer<T> Create(Func<T, T, int> comparer)
    {
        return new FunctionalComparer<T>(comparer);
    }
    public int Compare(T x, T y)
    {
        return comparer(x, y);
    }
}


//Returns the set of solutions for a given grid, for a certain target and solving type
//For each cell of the grid, the function RecursiveSolveSearch is called 
//and the resulting solutions are added to the value to be returned
public static HashSet<Solution> Solve(int target, CellBase[,] grid, Main.SolvingDelegate Solving)
{
    int initialValue = (solvingDelegate == Main.Addition) ? (0) : (1);
    solvingIteration = 0;

    int sum;
    HashSet<Solution> solutions = new HashSet<Solution>();

    for (int i = 0; i < grid.GetLength(0); i++)
    {
        for (int j = 0; j < grid.GetLength(1); j++)
        {
            if(solverThread.ThreadState == ThreadState.AbortRequested)
            {
                Debug.Log("Aborting solver thread");
                return solutions;
            }

            //initialize
            sum = initialValue;
            HashSet<Solution> solutionsFound = RecursiveSolveSearch(i, j, new Solution(), sum, target, grid, Solving);

            if (solutionsFound != null)
                solutions.UnionWith(solutionsFound);
        }
    }

    Debug.Log("Solver iterated cells: " + solvingIteration + " and solutions found: " + solutions.Count);
    LogSolutions(solutions);

    return solutions;
}

//Recursive function, that starts on a given cell, and find solutions for a target and solving type, 
//jumping to other neighbouring cells using a deep-first approach
static HashSet<Solution> RecursiveSolveSearch(int i, int j, Solution currentSolution, 
                                                    int sum, int result, CellBase[,] grid, Main.SolvingDelegate Solving)
{
    if (i < 0 || i >= grid.GetLength(0) || j < 0 || j >= grid.GetLength(1) || currentSolution.Contains(grid[i, j]))
        return null;

    sum = Solving(sum, grid[i, j].Value);

    //Base conditions, sum being higher than result, or sum being the result.
    //First case, return null as an indicator of invalid solution
    //Secound case, create the solution set
    if (sum > result)
        return null;

    currentSolution.Add(grid[i, j]);

    if (sum == result)
    {
        //Found Solution, return it
        var successfullSolution = new HashSet<Solution>();
        successfullSolution.Add(currentSolution);
        return successfullSolution;
    }

    //Iterative case, sum has not reached the result
    //Iterate all 8 possible neighboors paths
    HashSet<Solution> solutionsFoundTotal = null;

    for (int u = -1; u <= 1; ++u)
    {
        for (int v = -1; v <= 1; ++v)
        {
            //Canceling Background worker mid iteration cycle
            if (solverThread.ThreadState == ThreadState.AbortRequested)
            {
                Debug.Log("Aborting solver thread");
                return null;
            }

            //Static field, used to manage how many iterations required to solve the puzzle and
            //also to limit the maximum number of possible iterations
            solvingIteration++;

            //Dont use the same cell
            if (u == 0 && v == 0)
                continue;

            //Create new solution
            Solution newSolution = new Solution();
            newSolution.UnionWith(currentSolution);
            HashSet<Solution> solutionsFound = RecursiveSolveSearch(i + u, j + v, newSolution, sum, result, grid, Solving);

            //If a solution is found on any possible path, add this cell to it, and return the list of found solutions
            if (solutionsFound != null)
            {
                if (solutionsFoundTotal == null)
                    solutionsFoundTotal = new HashSet<Solution>();

                solutionsFoundTotal.UnionWith(solutionsFound);
            }
        }
    }

    //Return the solutions found
    return solutionsFoundTotal;
}
