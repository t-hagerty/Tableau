using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace tableau
{
    /// <summary>
    /// Abstract class for the data structures of a simplex and a Tucker tableau
    /// </summary>
    /// <see cref="http://math.uww.edu/~mcfarlat/s-prob.htm"/> 
    /// <see cref="http://www.ams.sunysb.edu/~tucker/Tuckertableau.pdf"/>
    abstract class Tableau
    {
        private int numRows;
        private int numCols;
        private double[,] tableau;
        private String[] variablePos;
        private ArrayList history = new ArrayList(); //An arraylist of integer pairs/coordinates that represent locations where the user pivoted 
        //(pivoting in the same spot reverses the pivot, so going backwards and pivoting again in the same spots will bring us to 
        //earlier tableaus)
        private int historyIndex = 0; //Keeps track of the spot in the history arraylist we're currently at. If user goes back in history,
        //index decrements, if goes back forwards, increments, or if the user instead picks a new pivot, overwrites the prev "future"
        //history with new history at the indices after the historyIndex
        private int undoCount = 0;

        /// <summary>
        /// Constructor for a Tableau object, in either the simplex or Tucker form. The tableau is a representation of a 
        /// linear programming maximization problem in a way that's easier for humans to solve.
        /// </summary>
        public Tableau() { }

        /// <summary>
        /// Pivots the tableau at a chosen element
        /// </summary>
        /// <param name="pivotRow">The row of the pivot element</param>
        /// <param name="pivotCol">The column of the pivot element</param>
        protected abstract void calcPivot(int pivotRow, int pivotCol);

        /// <summary>
        /// Performs the pivot operation at the selected coordinates for the user. Also updates the history. Different from
        /// the calcPivot method because calcPivot doesn't update history (so that we can use it for reversions as well)
        /// </summary>
        /// <param name="pivotRow">The row of the pivot element</param>
        /// <param name="pivotCol">The column of the pivot element</param>
        public void pivot(int pivotRow, int pivotCol)
        {
            if (tableau[pivotRow, pivotCol] == 0)
            {
                //Can't pivot on a 0, else there's all sorts of dividing by zero
                return;
            }

            calcPivot(pivotRow, pivotCol);
            
            //Add position where we pivoted to the history as a pair/set of coordinates
            Tuple<int, int> pivotCoord = new Tuple<int, int>(pivotRow, pivotCol);
            history.Insert(historyIndex, pivotCoord);
            historyIndex++;

            undoCount = 0; //If there was history in the "future" that we undid but were able to redo, we are no longer able to because we've done a new pivot instead
        }

        /// <summary>
        /// Allows the user to undo a pivot they've made, returning the tableau to previos states (using the history of pivots
        /// made, and the fact that pivoting an element that was just pivoted on reverses the original pivot).
        /// 
        /// Can't undo if there's no previous pivots to pivot to.
        /// </summary>
        public void undo()
        {
            if(historyIndex <= 0)
            {
                return; //No history to undo to, cancel the undo by returning out of the method.
            }

            historyIndex--;

            Tuple<int, int> pivotToUndo = (Tuple<int, int>)history[historyIndex];

            calcPivot(pivotToUndo.Item1, pivotToUndo.Item2);

            //Don't delete the pivot that was undone from the history, in case the user wants to redo it

            undoCount++; //Keep track of how many times we've used undo so that we can redo no more than this number of times
        }

        /// <summary>
        /// Redoes the next pivot in the history that a user previously did, but undid. Should only be available to be done
        /// if the user has undone a number of pivots (without doing a new pivot afterwards, that resets the history's "future")
        /// that can be redone, i.e: if the user performs the undo operation 3 times, they should be able to redo no more than 
        /// 3 times.
        /// </summary>
        public void redo()
        {
            if (undoCount > 0)
            {
                try
                {
                    Tuple<int, int> pivotToRedo = (Tuple<int, int>)history[historyIndex];

                    calcPivot(pivotToRedo.Item1, pivotToRedo.Item2);

                    historyIndex++;
                    undoCount--;
                }
                catch (Exception e)
                {
                    Console.Write(e);
                }
            }
        }

        /// <summary>
        /// Converts a simplex tableau to a Tucker tableau form, or vice versa
        /// </summary>
        public abstract Tableau convert();

        /// <summary>
        /// Prints out a String of the tableau's elements in the form of a tableau
        /// </summary>
        /// <returns>A String representing the tableau and its elements organized in a fashion that should resemble the tableau</returns>
        public String toString()
        {
            String tableauString = "";

            for(int r = 0; r < numRows - 1; r++)
            {
                for(int c = 0; c < numCols - 1; c++)
                {
                    tableauString += tableau[r, c] + " ";
                }

                tableauString += "| " + tableau[r, numCols - 1] + "\n";
            }

            for(int c = 0; c <= numCols; c++)
            {
                tableauString = tableauString + "--";
            }
            tableauString += "\n";

            for (int c = 0; c < numCols - 1; c++)
            {
                tableauString += tableau[numRows - 1, c] + " ";
            }
            tableauString += "| " + tableau[numRows -1, numCols - 1] + "\n";

            return tableauString;
        }

        protected String[] setupVariablePos(String[] variables, int numVariables, int numConstraints)
        {
            for (int i = 0; i < numVariables; i++)
            {
                variables[i] = "x" + (i + 1);
            }
            for (int i = numVariables; i < numVariables + numConstraints; i++)
            {
                variables[i] = "t" + (i + 1);
            }

            return variables;
        }

        /// <summary>
        /// Returns the variable to be displayed in certain spots, depending on if it's a simplex or a Tucker tableau. Positions
        /// start at 0, 1, 2, ....
        /// 
        /// In a simplex tableau, the variables/slack variables are just displayed
        /// in order, and their positions never change (Ex: x1, x2, x3, t1, t2). But, in a Tucker tableau, only the original
        /// problem variables are displayed along the top initially, and the slack variables are off to the side. When we pivot,
        /// we swap the top variable with the one on the side. It's important to keep track of this in a Tucker tableau as it
        /// gives important information.
        /// </summary>
        /// <param name="pos">The column that the variable represents/the column that the variable would be above in our 
        /// tableau representation of the problem.</param>
        /// <returns>The variable at the input position, as a String</returns>
        public abstract String getVariableAtPos(int pos);

        /// <summary>
        /// Checks if we've finished and maximized the objective function of the tableau.
        /// </summary>
        /// <returns>True if we've finished and maximized the objective function, false otherwise</returns>
        public abstract bool isMaximized();

        /// <summary>
        /// Tests for feasibility of the tableau/solution so the program can stop the user from making more pivots that either
        /// won't get them anywhere (cycles, going backwards, etc.) or will cause illegal operations.
        /// </summary>
        /// <returns>True if the tableau/solution is feasible and we can keep on pivoting and trying to solve the objective function,
        /// false if any tests for infeasibility fail.</returns>
        public bool isFeasible()
        {
            //TO DO

            return true;
        }
    }
}
