using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace tableau
{
    /// <summary>
    /// Data structure representing the simplex tableau that the user manipulates
    /// </summary>
    /// <see cref="http://math.uww.edu/~mcfarlat/s-prob.htm"/> 
    /// <see cref="http://www.ams.sunysb.edu/~tucker/Tuckertableau.pdf"/>
    class Tableau
    {
        private bool isSimplex; //Denotes whether or not the tableau is being represented in the simplex form or the Tucker form
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
        /// <param name="numConstraints">The number of constraint equations the objective function is subject to.</param>
        /// <param name="numVariables">The number of variables involved in the objective function (and the constraints)</param>
        /// <param name="isSimplexTableau">True if the tableau is to be represented in the simplex algorithm form, false if it
        /// is to be represented as a Tucker tableau</param>
        public Tableau(int numConstraints, int numVariables, bool isSimplexTableau)
        {
            isSimplex = isSimplexTableau;
            numRows = numConstraints + 1; //The number of rows in the tableau is the number of constraint equations + 1 (the objective function)
            
            //The number of columns in the tableau is the number of variables + number of constraint equations (from 
            //slack variables) + 1 if a SIMPLEX TABLEAU, or the number of variables + 1 if a TUCKER TABLEAU
            if (isSimplex)
            {
                numCols = numVariables + numConstraints + 1;
            }
            else
            {
                numCols = numVariables + 1;
            }

            tableau = new double[numRows, numCols];

            variablePos = new String[numVariables + numConstraints];

            for(int i = 0; i < numVariables; i++)
            {
                variablePos[i] = "x" + (i + 1);
            }
            for(int i = numVariables; i < numVariables + numConstraints; i++)
            {
                variablePos[i] = "t" + (i + 1);
            }
        }

        /// <summary>
        /// Pivots the tableau at a chosen element according to the rules of the tucker tableau algorithm for linear programming
        /// </summary>
        /// <param name="pivotRow">The row of the pivot element</param>
        /// <param name="pivotCol">The column of the pivot element</param>
        private void tuckerPivot(int pivotRow, int pivotCol)
        {
            if(tableau[pivotRow, pivotCol] == 0)
            {
                //Can't pivot on a 0, else there's all sorts of dividing by zero
                return;
            }
            
            for(int r = 0; r < numRows; r++)
            {
                if(r == pivotRow)
                {
                    continue; //Make sure we don't alter any elements in the pivot row because those elements are altered differently
                }

                for(int c = 0; c < numCols; c++)
                {
                    /*
                     * if element is not in the pivot col, call element s
                     * let element in the same row as s but in the pivot column be called r.
                     * let element in the same column as s but in the pivot row be called q.
                     * Then, after pivoting, s becomes: s - (r*q)/p, where p is the pivot element.
                     * 
                     * else if element is in the pivot column, let element be r
                     * element after pivoting will be -r/p, where p = pivot element
                     * _____________________               _______________________________
                     * | r ... s ... | ... |               | -r/p ... s - rq/p ... | ... |
                     * | .     .     |     |               |   .         .         |     |
                     * | p ... q ... | ... |     >>>>>>>   |  1/p ...   q/p    ... | ... |
                     * | .     .     |     |               |   .         .         |     |
                     * ---------------------               -------------------------------
                     * |_____________|_____|               |_______________________|_____|
                     */
                    if(c != pivotCol)
                    {
                        tableau[r, c] -= (tableau[r, pivotCol] * tableau[pivotRow, c]) / tableau[pivotRow, pivotCol];
                    }
                    else
                    {
                        tableau[r, c] = (tableau[r, c] * -1) / tableau[pivotRow, pivotCol];
                    }
                }
            }

            //For all elements q != p (the pivot element) in the pivot row, q becomes q/p after pivoting:
            for(int c = 0; c < pivotCol; c++)
            {
                tableau[pivotRow, c] /= tableau[pivotRow, pivotCol];
            }
            //Broken up into two for loops so we don't need to check each time to make sure we're not changing the pivot element
            for (int c = pivotCol + 1; c < numCols; c++)
            {
                tableau[pivotRow, c] /= tableau[pivotRow, pivotCol];
            }

            tableau[pivotRow, pivotCol] = 1 / tableau[pivotRow, pivotCol]; //pivot element p becomes 1/p

            String temp = variablePos[pivotCol];
            variablePos[pivotCol] = variablePos[numCols + pivotRow];
            variablePos[numCols + pivotRow] = temp;
        }

        /// <summary>
        /// Pivots the tableau at a chosen element according to the rules of the simplex algorithm for linear programming
        /// </summary>
        /// <param name="pivotRow">The row of the pivot element</param>
        /// <param name="pivotCol">The column of the pivot element</param>
        private void simplexPivot(int pivotRow, int pivotCol)
        {
            if (tableau[pivotRow, pivotCol] == 0)
            {
                //Can't pivot on a 0, else there's all sorts of dividing by zero
                return;
            }
            /*
             * For the simlex algorithm pivot: 
             * 1. We divide every element in the same row as the pivot by the pivot element (including
             * the pivot element, so it now equals 1)
             * 2. We subtract each element in the pivot row multiplied by the element in the row we're subtracting from but
             * in the pivot column, from the corresponding element in that row.
             * 
             * FOR EXAMPLE:
             * 
             *  2  1 1 1 0 0 | 14          8/5  0 0 1 0 -1/5 | 8
             *  4  2 3 0 1 0 | 28          16/5 0 1 0 1 -2/5 | 16
             *  2 *5 5 0 0 1 | 30   >>>>>  2/5  1 1 0 0  1/5 | 6
             * ------------------          ----------------------
             * -1 -2 1 0 0 0 | 0           -1/5 0 3 0 0  2/5 | 12
             * 
             * Pivoting on *5 in the first tableau, the row operations we do are:
             * r3 * 1/5 = r3
             * r1 - (1)r3 = r1
             * r2 - (2)r3 = r2
             * r4 - (-2)r3 = r4
             * and thus, every element in the pivot column should = 0 other tha th pivot element, which should = 1
             */
            for(int c = 0; c < numCols; c++)
            {
                tableau[pivotRow, c] /= tableau[pivotRow, pivotCol];
            }

            for(int r = 0; r < pivotRow; r++)
            {
                double multiplier = tableau[r, pivotCol];

                for(int c = 0; c < numCols; c++)
                {
                    tableau[r, c] -= multiplier * tableau[pivotRow, c];
                }
            }
            //For loop broken up into two parts so we can skip the pivotRow without having to check for it every time.
            for (int r = pivotRow + 1; r < numRows; r++)
            {
                double multiplier = tableau[r, pivotCol];

                for (int c = 0; c < numCols; c++)
                {
                    tableau[r, c] -= multiplier * tableau[pivotRow, c];
                }
            }

            //Variables don't move in simplex, but this makes it easier to convert to tucker, and our function will return 
            //variables unmoved for simplex, so we can safely switch them here
            String temp = variablePos[pivotCol];
            variablePos[pivotCol] = variablePos[numCols + pivotRow];
            variablePos[numCols + pivotRow] = temp;
        }

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

            if (!isSimplex)
            {
                tuckerPivot(pivotRow, pivotCol);
            }
            else
            {
                simplexPivot(pivotRow, pivotCol);
            }

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

            if (!isSimplex)
            {
                tuckerPivot(pivotToUndo.Item1, pivotToUndo.Item2);
            }
            else
            {
                simplexPivot(pivotToUndo.Item1, pivotToUndo.Item2);
            }

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

                    if (!isSimplex)
                    {
                        tuckerPivot(pivotToRedo.Item1, pivotToRedo.Item2);
                    }
                    else
                    {
                        simplexPivot(pivotToRedo.Item1, pivotToRedo.Item2);
                    }

                    historyIndex++;
                    undoCount--;
                }
                catch (Exception e)
                {

                }
            }
        }

        /// <summary>
        /// Converts a simplex tableau to a Tucker tableau form, or vice versa
        /// </summary>
        public void convert()
        {
            if(isSimplex)
            {
                convertToTucker();
            }
            else
            {
                convertToSimplex();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void convertToTucker()
        {
            //TO DO
        }

        /// <summary>
        /// 
        /// </summary>
        private void convertToSimplex()
        {
            //TO DO
        }

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
        public String getVariableAtPos(int pos)
        {
            if(!isSimplex)
            {
                return variablePos[pos];
            }
            //else
            if (pos > numCols - numRows) //Slack variable
            {
                return "t" + (pos - (numCols - numRows -1));
            }
            else //x variable
            {
                return "x" + (pos + 1); 
            }
        }

        /// <summary>
        /// Checks a simplex tableau for if we've finished and maximized the objective function (when values of all variables 
        /// in obj. func. >= 0)
        /// </summary>
        /// <returns>True if we've finished and maximized the objective function, false otherwise</returns>
        private bool isSimplexMaximized()
        {
            for(int c = 0; c < numCols -1; c++)
            {
                if(tableau[numRows-1, c] < 0)
                {
                    return false;
                }
            }

            return true; //All values in the objective function row (besides the answer we want to maximize, the value in the
            //bottom right corner) are greater than or = to zero, so this is our FINAL tableau and we have our maximum value
        }

        /// <summary>
        /// Checks a Tucker tableau for if we've finished and maximized the objective function (when values of all variables 
        /// in obj. func. <= 0)
        /// </summary>
        /// <returns>True if we've finished and maximized the objective function, false otherwise</returns>
        private bool isTuckerMaximized()
        {
            for (int c = 0; c < numCols - 1; c++)
            {
                if (tableau[numRows - 1, c] > 0)
                {
                    return false;
                }
            }

            return true; //All values in the objective function row (besides the answer we want to maximize, the value in the
            //bottom right corner) are less than or equal to zero, so the Tucker tableau is maximized.
        }

        /// <summary>
        /// Checks if we've finished and maximized the objective function of the tableau.
        /// </summary>
        /// <returns>True if we've finished and maximized the objective function, false otherwise</returns>
        public bool isMaximized()
        {
            if(isSimplex)
            {
                return isSimplexMaximized();
            }
            else
            {
                return isTuckerMaximized();
            }
        }

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
