using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace tableau
{
    /// <summary>
    /// Data structure representing a Simplex tableau, provides definitions for the operations one would
    /// do on the tableau data structure following the rules of the Simplex algorithm
    /// </summary>
    /// /// <see cref="http://math.uww.edu/~mcfarlat/s-prob.htm"/> 
    class SimplexTableau : Tableau
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
        /// Constructor for a Tableau object in the simplex form. The tableau is a representation of a 
        /// linear programming maximization problem in a way that's easier for humans to solve.
        /// </summary>
        /// <param name="numConstraints">The number of constraint equations the objective function is subject to.</param>
        /// <param name="numVariables">The number of variables involved in the objective function (and the constraints)</param>
        public SimplexTableau(int numConstraints, int numVariables)
        {
            numRows = numConstraints + 1; //The number of rows in the tableau is the number of constraint equations + 1 (the objective function)
            
            //The number of columns in the tableau is the number of variables + number of constraint equations (from 
            //slack variables) + 1 if a SIMPLEX TABLEAU
            numCols = numVariables + numConstraints + 1;

            tableau = new double[numRows, numCols];

            variablePos = new String[numVariables + numConstraints];

            setupVariablePos(variablePos, numVariables, numConstraints);
        }

        /// <summary>
        /// Constructor for a Simplex tableau, given a Tucker tableau. Used to convert a Tucker to a simplex tableau.
        /// </summary>
        /// <param name="tableau">A tableau in the Tucker form that is to be converted to a simplex tableau</param>
        public SimplexTableau(TuckerTableau tableau)
        {

        }

        /// <summary>
        /// Pivots the tableau at a chosen element according to the rules of the simplex algorithm for linear programming
        /// </summary>
        /// <param name="pivotRow">The row of the pivot element</param>
        /// <param name="pivotCol">The column of the pivot element</param>
        protected override void calcPivot(int pivotRow, int pivotCol)
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
            for (int c = 0; c < numCols; c++)
            {
                tableau[pivotRow, c] /= tableau[pivotRow, pivotCol];
            }

            for (int r = 0; r < pivotRow; r++)
            {
                double multiplier = tableau[r, pivotCol];

                for (int c = 0; c < numCols; c++)
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
        /// Converts this simplex tableau to a Tucker tableau form
        /// </summary>
        public override Tableau convert()
        {
            return new TuckerTableau(this);
        }

        /// <summary>
        /// Returns the variable to be displayed in certain spots. Positions start at 0, 1, 2, ....
        /// 
        /// In a simplex tableau, the variables/slack variables are just displayed
        /// in order, and their positions never change, unlike a Tucker tableau (Ex: x1, x2, x3, t1, t2).
        /// </summary>
        /// <param name="pos">The column that the variable represents/the column that the variable would be above in our 
        /// tableau representation of the problem.</param>
        /// <returns>The variable at the input position, as a String</returns>
        public override String getVariableAtPos(int pos)
        {
            if (pos > numCols - numRows) //Slack variable
            {
                return "t" + (pos - (numCols - numRows - 1));
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
        public override bool isMaximized()
        {
            for (int c = 0; c < numCols - 1; c++)
            {
                if (tableau[numRows - 1, c] < 0)
                {
                    return false;
                }
            }

            return true; //All values in the objective function row (besides the answer we want to maximize, the value in the
            //bottom right corner) are greater than or = to zero, so this is our FINAL tableau and we have our maximum value
        }
    }
}
