// ***********************************************************************
// Assembly         : Noob.DataStructures
// Author           : noob
// Created          : 2023-02-01
// SourceLink       ：https://www.gyanblog.com/coding-interview/young-tableau-implementation
//
// Last Modified By : noob
// Last Modified On : 2023-02-01
// ***********************************************************************
// <copyright file="YoungTableauTests.cs" company="Noob.DataStructures">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

/// <summary>
/// The DataStructures namespace.
/// </summary>
namespace Noob.DataStructures
{
    /// <summary>
    /// Class YoungTableauTests.
    /// </summary>
    public class YoungTableauTests
    {
        /// <summary>
        /// The matrix
        /// </summary>
        private int[,] matrix;
        /// <summary>
        /// The size
        /// </summary>
        private int size;

        /// <summary>
        /// Initializes a new instance of the <see cref="YoungTableauTests"/> class.
        /// </summary>
        /// <param name="matrix">The matrix.</param>
        /// <param name="size">The size.</param>
        public YoungTableauTests(int[,] matrix, int size)
        {
            this.matrix = matrix;
            this.size = size;

            //build young tableau from given matrix
            this.buildYoungTableau();
        }

        /// <summary>
        /// Builds the young tableau.
        /// </summary>
        private void buildYoungTableau()
        {
            for (int i = this.size - 1; i >= 0; i--)
            {
                for (int j = this.size - 1; j >= 0; j--)
                {
                    this.tableau_heapify(i, j);
                }
            }
        }


        /// <summary>
        /// Main the property of young tableau
        /// </summary>
        /// <param name="r">The r.</param>
        /// <param name="c">The c.</param>
        private void tableau_heapify(int r, int c)
        {
            int[] left = this.getLeft(r, c);
            int[] right = this.getRight(r, c);

            int[] smallest = new int[] { r, c };

            // System.out.println(r + ", " + c);
            //check if the cell is less than both of its children
            if (this.isCellValid(left) && this.matrix[left[0], left[1]] < this.matrix[r, c])
            {
                //save address of left cell
                smallest = left;
            }

            if (this.isCellValid(right) && this.matrix[right[0], right[1]] < this.matrix[smallest[0], smallest[1]])
            {
                //save address of right cell
                smallest = right;
            }

            //Check if we found changed address in our smallest variables.
            //If yes, swap them. And, recursively call this method on that cell address
            if (this.isCellValid(smallest) && (smallest[0] != r || smallest[1] != c))
            {
                //swap
                int t = this.matrix[r, c];
                this.matrix[r, c] = this.matrix[smallest[0], smallest[1]];
                this.matrix[smallest[0], smallest[1]] = t;

                // recursive call tableau_heapify
                this.tableau_heapify(smallest[0], smallest[1]);
            }
        }

        /// <summary>
        /// Return [-1,-1], if there is no child element
        /// </summary>
        /// <param name="r">The r.</param>
        /// <param name="c">The c.</param>
        /// <returns>System.Int32[].</returns>
        private int[] getLeft(int r, int c)
        {
            if (r >= this.size - 1)
            {
                return new int[] { -1, -1 };
            }

            return new int[] { r + 1, c };
        }

        /// <summary>
        /// Return [-1,-1], if there is no child element
        /// </summary>
        /// <param name="r">The r.</param>
        /// <param name="c">The c.</param>
        /// <returns>System.Int32[].</returns>
        private int[] getRight(int r, int c)
        {
            if (c >= this.size - 1)
            {
                return new int[] { -1, -1 };
            }
            return new int[] { r, c + 1 };
        }

        /// <summary>
        /// Check if the cell is within the matrix
        /// </summary>
        /// <param name="cell">The cell.</param>
        /// <returns><c>true</c> if [is cell valid] [the specified cell]; otherwise, <c>false</c>.</returns>
        private bool isCellValid(int[] cell)
        {
            return cell[0] >= 0 && cell[0] < this.size && cell[1] >= 0 && cell[1] < this.size;
        }

        /// <summary>
        /// Get element at (0,0), and make matrix tableau_heapify again
        /// </summary>
        /// <returns>System.Int32.</returns>
        public int extractMin()
        {
            int min = this.matrix[0, 0];

            //put a max value here, just to denote that it has been taken out.
            this.matrix[0, 0] = int.MaxValue;

            //lets maintain tableau property
            this.tableau_heapify(0, 0);

            //all is set
            return min;
        }

        /// <summary>
        /// a recursive method to search an element
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="currentCell">The current cell.</param>
        /// <returns>return null if not found</returns>
        private int[] search(int element, int[] currentCell)
        {
            if (!this.isCellValid(currentCell))
            {
                return null;
            }
            int currentElement = this.matrix[currentCell[0], currentCell[1]];
            if (element == currentElement)
            {
                return currentCell;
            }

            if (element < currentElement)
            {
                // move left
                // decrement col by 1
                currentCell[1]--;
                return this.search(element, currentCell);
            }
            else
            { //(element > currentElement) {
              //move down
              //increment row by 0
                currentCell[0]++;
                return this.search(element, currentCell);
            }
        }

        /// <summary>
        /// Search the given element and return its index in matrix
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns>System.Int32[].</returns>
        public int[] searchElement(int element)
        {
            //we will start from top right corner to find the element
            return this.search(element, new int[] { 0, this.size - 1 });
        }

        public void Test()
        {
            int[,] matrix = {
                {12, 7, 11},
                {8, 9, 1},
                {4, 5, int.MaxValue}
            };


        }
    }
}
