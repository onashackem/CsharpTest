using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace Excel
{
    class Excel
    {
        static void Main(string[] args)
        {
            string INPUT_SHEET, OUTPUT_SHEET;

            if (!ProcessArguments(args, out INPUT_SHEET, out OUTPUT_SHEET))
            {
                Console.WriteLine("Argument Error");
                return;
            }

            try
            {
                new ExcelProcessor().Process(INPUT_SHEET, OUTPUT_SHEET);
                ;
            }
            catch (Exception e)
            {
                // Not an argument error -> file error
                Console.WriteLine("File Error");
            }
        }

        /// <summary>
        /// Extracts input and output file from arguments.
        /// </summary>
        /// <param name="args">Arguments collection</param>
        /// <param name="inputFile">Extracted input file</param>
        /// <param name="outputFile">Extracted output file</param>
        /// <returns>Returns false if something doesn't fit, true otherwise</returns>
        private static bool ProcessArguments(string[] args, out string inputFile, out string outputFile)
        {
            // Intial values
            inputFile = outputFile = string.Empty;

            // Check arguments length
            if (args.Length != 2)
            {
                return false;
            }

            inputFile = args[0];
            outputFile = args[1];

            // Check valid paths
            if (String.IsNullOrEmpty(inputFile) || String.IsNullOrEmpty(outputFile))
            {
                return false;
            }

            // Input parameter is OK
            return true;
        }
    }

    class ExcelProcessor
    {
        private readonly LinkedList<Cell[]> cellRows = new LinkedList<Cell[]>();

        public static readonly string EMPTY_CELL_VALUE = "[]";

        private readonly char[] CELL_DELIMITER = new char[] { ' ' };

        private string INPUT_SHEET_NAME;

        private string EXCEL_SHEETS_PATH;

        public void Process(string inputSheet, string outputSheet)
        {
            int pathLength = Math.Max(inputSheet.LastIndexOf('\\'), inputSheet.LastIndexOf('/'));
            EXCEL_SHEETS_PATH = inputSheet.Substring(0, pathLength + 1);
            INPUT_SHEET_NAME = inputSheet.Remove(inputSheet.Length - 6).Replace(EXCEL_SHEETS_PATH, "");            

            using(var reader = new StreamReader(inputSheet))
            using(var writer = new StreamWriter(outputSheet))
            {
                ReadInputSheet(reader);

                EvaluateAndWriteCells(writer);
            }
        }

        public Cell FindCell(string identifier)
        {
            // Match inentifier with proper cell regex
            var match = Regex.Match(identifier, @"([^:]+:)?([A-Z]+)([0-9]+)");

            // Invalid cell identifier
            if (!match.Success)
            {
                return null;
            }

            // Row index starts from 1 but indexinf start from 0
            int rowIndex = Convert.ToInt32(match.Groups[3].ToString()) - 1;
            int columnIndex = GetColumnIndexFromColumnName(match.Groups[2].ToString().ToUpper());
            int colonIndex = identifier.IndexOf(":");

            // If cell identifier contains sheet name ...
            if (colonIndex > 0)
            {
                var sheetName = identifier.Substring(0, colonIndex);

                // ... and it is not an input sheet, find the cell in the another file
                if (!sheetName.Equals(INPUT_SHEET_NAME))
                    return FindCellInSheet(sheetName, columnIndex, rowIndex);
            }

            // RowIndex out of bounds
            if (cellRows.Count <= rowIndex)
                return new ZeroValueCell();

            // ColumnIndex out of bounds
            if (cellRows.ElementAt(rowIndex).Length <= columnIndex)
                return new ZeroValueCell();

            // Return row
            return cellRows.ElementAt(rowIndex)[columnIndex];
        }

        /// <summary>
        /// Converts cell character column indentifier into integer. A -> 1, B -> 2, etc.
        /// </summary>
        /// <param name="columnName">String column name</param>
        /// <returns>Returns int column index</returns>
        private int GetColumnIndexFromColumnName(string columnName)
        {
            int index = 0;
            int power = 1;
            foreach (var c in columnName.Reverse())
            {
                // Horner with base 26
                index += ((int)c - (int)'A') * power;
                power *= 26;
            }

            return index;
        }

        /// <summary>
        /// Finds cell in the special sheet file
        /// </summary>
        /// <param name="sheet">Name of the sheet</param>
        /// <param name="columnIndex">Column index of the cell</param>
        /// <param name="rowIndex">Row index if the cell</param>
        /// <returns>Returns cell if is found</returns>
        private Cell FindCellInSheet(string sheet, int columnIndex, int rowIndex)
        {
            try
            {
                using(var reader = new StreamReader(String.Format("{0}{1}.sheet", EXCEL_SHEETS_PATH, sheet)))
                {
                    // Skip the proper number on lines
                    string line = String.Empty;
                    while(--rowIndex >= 0)
                    {
                        line = reader.ReadLine();
                    }

                    // Get cells on the line
                    string[] cells = line.Split(CELL_DELIMITER, StringSplitOptions.RemoveEmptyEntries);

                    // Find proper cell
                    if (cells.Length < columnIndex)
                    {
                        return CreateCell(cells[columnIndex - 1]);
                    }

                    return new ZeroValueCell();
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private void EvaluateAndWriteCells(StreamWriter writer)
        {
            foreach (var cellRow in cellRows)
            {
                foreach (var cell in cellRow)
                {
                    // PrintValue prints lazy-evaluated value
                    writer.Write(cell.PrintValue);

                    // Sepatare cell with space
                    if (cell != cellRow[cellRow.Length - 1])
                    {
                        writer.Write(" ");
                    }
                }

                writer.WriteLine();
            }
        }

        private void ReadInputSheet(StreamReader reader)
        {
            string cellLine;
            while((cellLine = reader.ReadLine()) != null)
            {
                var splittedCellLine = cellLine.Split(CELL_DELIMITER, StringSplitOptions.RemoveEmptyEntries);                
                var cells = new Cell[splittedCellLine.Length];
                
                int index = 0;
                foreach(var value in splittedCellLine)
                {
                    cells[index++] = CreateCell(value);
                }

                cellRows.AddLast(cells);
            }
        }

        private Cell CreateCell(string value)
        {
            Cell cell;
            if (EMPTY_CELL_VALUE == value)
            {
                cell = new EmptyCell();
            }
            else if (value.StartsWith("="))
            {
                cell = new FormulaCell(this, value.Substring(1));
            }
            else
            {
                cell = new ValueCell(value);
            }

            return cell;
        }
    }

    #region Cells
    abstract class Cell
    {
        /// <summary>
        /// Contains already evalueated value (if is not null)
        /// </summary>
        protected int evaluatedValue = 0;

        /// <summary>
        /// Indicates whether the cell has already been evaluated
        /// </summary>
        protected bool isEvaluated = false;

        /// <summary>
        /// Contains some message if the evaluation failed
        /// </summary>
        protected string errorMessage = string.Empty;

        /// <summary>
        /// Indicates whether evaluation finished sucessfully
        /// </summary>
        public bool HasEvaluationSucceeded
        {
            get
            {
                return String.IsNullOrEmpty(errorMessage);
            }
        }

        /// <summary>
        /// Returns cell value that is printed to the output
        /// </summary>
        public virtual string PrintValue
        {
            get
            {
                if (!HasEvaluationSucceeded)
                {
                    return errorMessage;
                }

                return EvaluatedValue.ToString();
            }
        }

        /// <summary>
        /// Gets evaluated cell value
        /// </summary>
        public int EvaluatedValue
        {
            get
            {
                // Lazy evauation of the cell prevents from evaluating the value twice
                if (!isEvaluated)
                {
                    try
                    {
                        // Remember evaluated value
                        evaluatedValue = EvaluateValue();
                    }
                    catch(ExcelException ex)
                    {
                        // An exception during evaluation is also an cell value
                        errorMessage = ex.Message;
                    }

                    // No matter if evaluation fails, cell is be evaluated
                    isEvaluated = true;
                }

                // Return evaluated value
                return evaluatedValue;
            }
        }

        /// <summary>
        /// Virtual method to evalueate a cell value.
        /// A proper ExcelException should be thrown when an error occured.
        /// </summary>
        /// <returns>Returns evaluated cell value</returns>
        protected abstract int EvaluateValue();
    }

    /// <summary>
    /// Represents and epmty cell - []
    /// </summary>
    class EmptyCell : Cell
    {
        public EmptyCell() 
            : base()
        {
            // This is already evaluated
            isEvaluated = true;
        }

        public override string PrintValue
        {
            get
            {
                return ExcelProcessor.EMPTY_CELL_VALUE;
            }
        }

        protected override int EvaluateValue()
        {
            throw new InvalidOperationException("This method is not supposed to be called!");
        }
    }

    /// <summary>
    /// Represents non-existing cell
    /// </summary>
    class ZeroValueCell : Cell
    {
        public ZeroValueCell()
            : base()
        {
            // This is already evaluated
            isEvaluated = true;
        }

        public override string PrintValue
        {
            get
            {
                throw new InvalidOperationException("Zero value cell should not be printed at all!");
            }
        }

        protected override int EvaluateValue()
        {
            throw new InvalidOperationException("This method is not supposed to be called!");
        }
    }

    /// <summary>
    /// Represents a cell with an int or string value
    /// </summary>
    class ValueCell : Cell
    {
        public ValueCell(string value)
            : base()
        {
            // Value can be either int or string
            int intValue;
            if (Int32.TryParse(value, out intValue))
            {
                evaluatedValue = intValue;
            }
            else
            {
                errorMessage = new ExcelInvalidValueException().Message;
            }

            // Value is either int (is parsed) or string (error is set)
            isEvaluated = true;
        }

        protected override int EvaluateValue()
        {
            throw new InvalidOperationException("This method is not supposed to be called!");
        }
    }

    /// <summary>
    /// Represents a formula in cell (= CellIndentifier Operator CellIdentifier)
    /// </summary>
    class FormulaCell : Cell
    {
        private ExcelProcessor processor;
        private String originalValue;
        private bool isBeingEvaluatedNow = false;

        public FormulaCell(ExcelProcessor processor, string value)
            : base()
        {
            this.processor = processor;
            this.originalValue = value;
        }

        protected override int EvaluateValue()
        {
            int indexOfPlus = originalValue.IndexOf('+');
            int indexOfMinus = originalValue.IndexOf('-');
            int indexOfMul = originalValue.IndexOf('*');
            int indexOfDiv = originalValue.IndexOf('/');

            // Check operator presence
            int operatorPosition = Math.Max(indexOfPlus, Math.Max(indexOfMinus, Math.Max(indexOfMul, indexOfDiv)));
            if (-1 == operatorPosition)
            {
                throw new ExcelMissingOperatorException();
            }

            // Check operans presence
            var firstOperand = originalValue.Substring(0, operatorPosition);
            var secondOperand = originalValue.Substring(operatorPosition + 1);
            if (String.IsNullOrEmpty(firstOperand) || String.IsNullOrEmpty(secondOperand))
            {
                throw new ExcelMissingOperatorException();
            }

            var firstCell = processor.FindCell(firstOperand);
            var secondCell = processor.FindCell(secondOperand);
            char operatorSign = this.originalValue[operatorPosition];

            // If any cell was not found (bad identifier)
            if (firstCell == null || secondCell == null)
            {
                throw new ExcelInvalidFormulaException();
            }

            return Evaluate(firstCell, secondCell, operatorSign);
        }

        private int Evaluate(Cell firstCell, Cell secondCell, char operatorSign)
        {
            string cycleMessage = new ExcelCycleException().Message;

            // Cycle detected
            if (isBeingEvaluatedNow)
            {
                errorMessage = cycleMessage;
                return 0;
            }

            // Cycle detection
            isBeingEvaluatedNow = true;

            int firstValue = firstCell.EvaluatedValue;
            int secondValue = secondCell.EvaluatedValue;

            // Stop cycle detection
            isBeingEvaluatedNow = false;

            if (firstCell.HasEvaluationSucceeded && secondCell.HasEvaluationSucceeded)
            {
                // Compute cell value
                switch (operatorSign)
                {
                    case '+':
                        return firstValue + secondValue;
                    case '-':
                        return firstValue - secondValue;
                    case '*':
                        return firstValue * secondValue;
                    case '/':
                        // Check for division by zero
                        if (0 == secondValue)
                        {
                            throw new ExcelDivisionByZeroException();
                        }

                        // Division is safe
                        return firstValue / secondValue;
                }
            }

            // Error occured
            if (firstCell.PrintValue == cycleMessage || secondCell.PrintValue == cycleMessage)
            {
                errorMessage = cycleMessage;
            }
            else
            {
                errorMessage = new ExcelErrorException().Message;
            }
            return 0;
        }
    }

    #endregion

    
    #region Exceptions

    class ExcelException : Exception
    {
        protected ExcelException(string message) : base (message) { }
    }

    class ExcelErrorException : ExcelException
    {
        public ExcelErrorException() : base("#ERROR") { }
    }

    class ExcelDivisionByZeroException : ExcelException
    {
        public ExcelDivisionByZeroException() : base("#DIV0") { }
    }

    class ExcelCycleException : ExcelException
    {
        public ExcelCycleException() : base("#CYCLE") { }
    }

    class ExcelMissingOperatorException : ExcelException
    {
        public ExcelMissingOperatorException() : base("#MISSOP") { }
    }

    class ExcelInvalidFormulaException : ExcelException
    {
        public ExcelInvalidFormulaException() : base("#FORMULA") { }
    }

    class ExcelInvalidValueException : ExcelException
    {
        public ExcelInvalidValueException() : base("#INVVAL") { }
    }

    #endregion
}
