using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace tableau
{
    /// <summary>
    /// Data structure representing a Tucker tableau, provides definitions for the operations one would
    /// do on the tableau data structure following the rules of the Tucker tableau algorithm
    /// </summary>
    /// <see cref="http://www.ams.sunysb.edu/~tucker/Tuckertableau.pdf"/>
    class TuckerTableau : Tableau
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
        /// Constructor for a new Tableau object in Tucker form. The tableau is a representation of a 
        /// linear programming maximization problem in a way that's easier for humans to solve.
        /// </summary>
        /// <param name="numConstraints">The number of constraint equations the objective function is subject to.</param>
        /// <param name="numVariables">The number of variables involved in the objective function (and the constraints)</param>
        public TuckerTableau(int numConstraints, int numVariables)
        {
            numRows = numConstraints + 1; //The number of rows in the tableau is the number of constraint equations + 1 (the objective function)
            
            //The number of columns in the tableau is the number of variables + 1 if a TUCKER TABLEAU
            numCols = numVariables + 1;

            tableau = new double[numRows, numCols];

            variablePos = new String[numVariables + numConstraints];

            setupVariablePos(variablePos, numVariables, numConstraints);
        }

        /// <summary>
        /// Constructor for a Tucker tableau, given a simplex tableau. Used to convert a simplex to a Tucker tableau.
        /// </summary>
        /// <param name="tableau">A tableau in the simplex form that is to be converted to a Tucker tableau</param>
        public TuckerTableau(SimplexTableau tableau)
        {

        }

        /// <summary>
        /// Pivots the tableau at a chosen element according to the rules of the tucker tableau algorithm for linear programming
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

            for (int r = 0; r < numRows; r++)
            {
                if (r == pivotRow)
                {
                    continue; //Make sure we don't alter any elements in the pivot row because those elements are altered differently
                }

                for (int c = 0; c < numCols; c++)
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
                    if (c != pivotCol)
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
            for (int c = 0; c < pivotCol; c++)
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
        /// Converts this Tucker tableau to a simplex tableau form
        /// </summary>
        public override Tableau convert()
        {
            return new SimplexTableau(this);
        }

        /// <summary>
        /// Returns the variable to be displayed in certain spots. Positions start at 0, 1, 2, ....
        /// 
        /// in a Tucker tableau, only the original problem variables are displayed along the top initially, and the slack
        /// variables are off to the side. When we pivot, we swap the top variable with the one on the side. It's important 
        /// to keep track of this in a Tucker tableau as it gives important information.
        /// </summary>
        /// <param name="pos">The column that the variable represents/the column that the variable would be above in our 
        /// tableau representation of the problem.</param>
        /// <returns>The variable at the input position, as a String</returns>
        public override String getVariableAtPos(int pos)
        {
            return variablePos[pos];
        }

        /// <summary>
        /// Checks a Tucker tableau for if we've finished and maximized the objective function (when values of all variables 
        /// in obj. func. <= 0)
        /// </summary>
        /// <returns>True if we've finished and maximized the objective function, false otherwise</returns>
        public override bool isMaximized()
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
    }
}
