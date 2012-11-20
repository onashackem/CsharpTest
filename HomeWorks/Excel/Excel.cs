using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections;

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

            // Make sure that we do not write into input file
            if (inputFile.Equals(outputFile))
            {
                return false;
            }

            // Both parameters are OK
            return true;
        }
    }

    /// <summary>
    /// Simplified Excel processor
    /// </summary>
    class ExcelProcessor
    {
        /// <summary>
        /// Value of empty-value cell
        /// </summary>
        public static readonly string EMPTY_CELL_VALUE = "[]";

        /// <summary>
        /// Indicates whether a cycle was detected
        /// </summary>
        public bool IsCycleDetected { get; private set; }

        /// <summary>
        /// Cells of the main sheet are loaded into the memory. 
        /// Allocated to 1000 rows to prevent from often realocation.
        /// </summary>
        private readonly List<Cell[]> mainSheet = new List<Cell[]>(1000);

        /// <summary>
        /// Cached cells from other sheets
        /// </summary>
        private readonly Hashtable cachedCells = new Hashtable();

        /// <summary>
        /// Cell delimiter
        /// </summary>
        private readonly char[] CELL_DELIMITER = new char[] { ' ' };

        /// <summary>
        /// Name of the main sheet
        /// </summary>
        private string INPUT_SHEET_NAME;

        /// <summary>
        /// The Cell that has finished the cycle
        /// </summary>
        private Cell cycleOriginator = null;

        /// <summary>
        /// Read the main sheet, compute all cells and write it to the output file
        /// </summary>
        /// <param name="inputSheet">Input sheet name</param>
        /// <param name="outputSheet">Output sheet name</param>
        public void Process(string inputSheet, string outputSheet)
        {
            // Remove .sheet extension
            INPUT_SHEET_NAME = inputSheet.Remove(inputSheet.Length - 6);            

            using(var reader = new StreamReader(inputSheet))
            using(var writer = new StreamWriter(outputSheet))
            {
                ReadInputSheet(reader);

                EvaluateAndWriteCells(writer);
            }
        }

        /// <summary>
        /// Finds cell by specified identifier - from input sheet or another sheet.
        /// </summary>
        /// <param name="identifier">Cell idetifier: sheet!COLUMNrow, sheet! is optional</param>
        /// <returns>Returns found cell or null</returns>
        public Cell FindCell(string identifier)
        {
            int exclamationMarkIndex = identifier.IndexOf("!");

            // Compute column
            int indexPosition = exclamationMarkIndex;
            int columnIndex = GetColumnIndex(identifier, ref indexPosition);

            // Compute row index
            --indexPosition;
            int rowIndex = GetRowIndex(identifier, ref indexPosition);

            if (columnIndex == 0 || rowIndex == 0)
            {
                // Either column or row was invalid
                return null;
            }

            // Get cell from sheet (Row and colls indexes start from 1 but collection indexing starts from 0)
            return FindCellInProperSheet(identifier, --columnIndex, --rowIndex);
        }

        /// <summary>
        /// When a cell knows it made a cycle in evaluation, it has to admit to it
        /// </summary>
        /// <param name="cell">Cell that caused the cycle</param>
        public void AdmitToCauseCycle(Cell cell)
        {
            IsCycleDetected = true;
            cycleOriginator = cell;
        }

        /// <summary>
        /// When a cycle is handled, the cell that caused the cell should stop notify the processor.
        /// </summary>
        /// <param name="cell"></param>
        public void CycleIsSolved(Cell cell)
        {
            IsCycleDetected = false;
            cycleOriginator = null;
        }

        /// <summary>
        /// Indicates whether a cell is the cell that caused the cycle
        /// </summary>
        /// <param name="cell">Cell candidate</param>
        /// <returns>Returns true if the cell caused the cycle</returns>
        public bool IsCycleOriginator(Cell cell)
        {
            return cell == cycleOriginator;
        }

        /// <summary>
        /// Finds cell by identifier on specified coordinates
        /// </summary>
        /// <param name="identifier">Cell identifier</param>
        /// <param name="columnIndex">Cell column index</param>
        /// <param name="rowIndex">Cell row index</param>
        /// <returns>Returns found cell</returns>
        private Cell FindCellInProperSheet(string identifier, int columnIndex, int rowIndex)
        {
            var sheetName = identifier.Substring(0, identifier.IndexOf('!'));

            // If sheet it is not the main sheet, find the cell in the another file and cache it fot further use ...
            if (!sheetName.Equals(INPUT_SHEET_NAME))
            {
                // Add to the cache
                if (!cachedCells.ContainsKey(identifier))
                    cachedCells.Add(identifier, FindCellInAnotherSheet(sheetName, columnIndex, rowIndex));

                // Return cached cell
                return (Cell)cachedCells[identifier];
            }

            // RowIndex out of bounds
            if (mainSheet.Count <= rowIndex)
                return new ZeroValueCell();

            // ColumnIndex out of bounds
            var cellRow = mainSheet[rowIndex];
            if (cellRow.Length <= columnIndex)
                return new ZeroValueCell();

            // Return row
            return cellRow[columnIndex];
        }

        private int GetRowIndex(string identifier, ref int position)
        {
            int rowIndex = 0;
            while (++position < identifier.Length)
            {
                if (identifier[position] >= '0' && identifier[position] <= '9')
                {
                    // Horner
                    rowIndex = rowIndex * 10 + ((int)identifier[position] - (int)'0');
                    continue;
                }

                // Invalid char
                return 0;
            }

            return rowIndex;
        }

        private int GetColumnIndex(string identifier, ref int position)
        {
            int columnIndex = 0;
            while (++position < identifier.Length)
            {
                if (identifier[position] >= 'A' && identifier[position] <= 'Z')
                {
                    // Horner
                    columnIndex = columnIndex * 26 + ((int)identifier[position] - (int)'A' + 1);
                    continue;
                }

                if (identifier[position] >= '0' && identifier[position] <= '9')
                {
                    // Valid end of column identifier
                    return columnIndex;
                }
                else
                {
                    // Invalid end of column identifier
                    return 0;
                }
            }

            return columnIndex;
        }

        /// <summary>
        /// Finds cell in the special sheet file
        /// </summary>
        /// <param name="sheetName">Name of the sheet</param>
        /// <param name="columnIndex">Column index of the cell</param>
        /// <param name="rowIndex">Row index if the cell</param>
        /// <returns>Returns cell if is found</returns>
        private Cell FindCellInAnotherSheet(string sheetName, int columnIndex, int rowIndex)
        {
            // Fix the difference between Excel indexing and collection indexing
            rowIndex += 1;

            try
            {
                using(var reader = new StreamReader(String.Format("{0}.sheet", sheetName)))
                {
                    // Skip the proper number on lines
                    string line = String.Empty;
                    while(!reader.EndOfStream && --rowIndex >= 0)
                    {
                        line = reader.ReadLine();
                    }

                    // Row index out of file
                    if (reader.EndOfStream && rowIndex > 0)
                    {
                        return new ZeroValueCell();
                    }

                    // Get cells on the line
                    string[] cells = line.Split(CELL_DELIMITER, StringSplitOptions.RemoveEmptyEntries);

                    // Find proper cell
                    if (columnIndex <= cells.Length)
                    {
                        return CreateCell(cells[columnIndex], sheetName);
                    }

                    // Column index out of row
                    return new ZeroValueCell();
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void EvaluateAndWriteCells(StreamWriter writer)
        {
            foreach (var cellRow in mainSheet)
            {
                foreach (var cell in cellRow)
                {
                    // PrintValue prints lazy-evaluated value
                    writer.Write(cell.PrintValue);

                    // Sepatare only cell with 1 space
                    if (cell != cellRow[cellRow.Length - 1])
                    {
                        writer.Write(" ");
                    }
                }

                // Do not end with a new line
                if (cellRow != mainSheet[mainSheet.Count - 1])
                {
                    writer.WriteLine();
                }
            }
        }

        private void ReadInputSheet(StreamReader reader)
        {
            string cellLine;
            while((cellLine = reader.ReadLine()) != null)
            {
                var splittedCellLine = cellLine.Split(CELL_DELIMITER, StringSplitOptions.RemoveEmptyEntries);                
                var cells = new Cell[splittedCellLine.Length];
                
                int index = -1;
                foreach(var value in splittedCellLine)
                {
                    cells[++index] = CreateCell(value, INPUT_SHEET_NAME);
                }

                mainSheet.Add(cells);
            }
        }

        private Cell CreateCell(string value, string sheetName)
        {
            Cell cell;
            if (EMPTY_CELL_VALUE.Equals(value))
            {
                cell = new EmptyCell();
            }
            else if (value[0].Equals('='))
            {
                cell = new FormulaCell(this, value.Substring(1), sheetName);
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
        protected int? evaluatedValue = null;

        /// <summary>
        /// Contains some message if the evaluation failed
        /// </summary>
        protected string errorMessage = ExcelExceptions.None;

        /// <summary>
        /// Indicates whether evaluation finished sucessfully
        /// </summary>
        public bool HasEvaluationSucceeded
        {
            get
            {
                return errorMessage.Equals(ExcelExceptions.None);
            }
        }

        /// <summary>
        /// Returns cell value that is printed to the output
        /// </summary>
        public virtual string PrintValue
        {
            get
            {
                // First evaluate cell if is not
                string evalueatedValue = EvaluatedValue.ToString();

                // Check for error during evaluation progress
                if (!HasEvaluationSucceeded)
                {
                    // Error occured -> show message
                    return errorMessage;
                }

                // Return evaluated value
                return evalueatedValue;
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
                if (!evaluatedValue.HasValue)
                {                   
                    // Remember evaluated value
                    evaluatedValue = EvaluateValue();
                }

                // Return evaluated value
                return evaluatedValue.Value;
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
            // Value of an empty cell is 0;
            evaluatedValue = 0;
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
            // Value of an empty cell is 0;
            evaluatedValue = 0;
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
            if (Int32.TryParse(value, out intValue) && intValue >= 0)
            {
                evaluatedValue = intValue;
            }
            else
            {
                errorMessage = ExcelExceptions.InvalidValueException;
                evaluatedValue = 0;
            }
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
        private string originalValue;
        private string sheetName;
        private bool isBeingEvaluated = false;

        public FormulaCell(ExcelProcessor processor, string value, string sheetName)
            : base()
        {
            this.processor = processor;
            this.originalValue = value;
            this.sheetName = sheetName;
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
                errorMessage = ExcelExceptions.MissingOperatorException;
                return 0;
            }

            // Check operand presence
            var firstOperand = originalValue.Substring(0, operatorPosition);
            var secondOperand = originalValue.Substring(operatorPosition + 1);
            if (String.IsNullOrEmpty(firstOperand) || String.IsNullOrEmpty(secondOperand))
            {
                errorMessage = ExcelExceptions.MissingOperatorException;
                return 0;
            }

            // Find cells in proper sheet (if sheet not specified, sheet of this cell is default)
            var firstCell = processor.FindCell(PrependSheetNameIfMissing(firstOperand, sheetName));
            var secondCell = processor.FindCell(PrependSheetNameIfMissing(secondOperand, sheetName));
            char operatorSign = this.originalValue[operatorPosition];

            // If any cell was not found (bad identifier)
            if (firstCell == null || secondCell == null)
            {
                errorMessage = ExcelExceptions.InvalidFormulaException;
                return 0;
            }

            // Evaluate current cell
            return Evaluate(firstCell, secondCell, operatorSign);
        }

        private string PrependSheetNameIfMissing(string operand, string sheetName)
        {
            //Sheet already present
            if (operand.IndexOf('!') > 0)
            {
                return operand;
            }

            // Prepend sheetname in proper format
            return String.Format("{0}!{1}", sheetName, operand);
        }

        private int Evaluate(Cell firstCell, Cell secondCell, char operatorSign)
        {
            // Cycle detected?
            if (isBeingEvaluated)
            {
                processor.AdmitToCauseCycle(this);

                errorMessage = ExcelExceptions.CycleException;
                return 0;
            }

            // Start cycle detection of this cell
            isBeingEvaluated = true;

            // Evaluate first and second cell
            int firstValue = firstCell.EvaluatedValue;
            int secondValue = secondCell.EvaluatedValue;
            
            // Stop cycle detection of this cell
            isBeingEvaluated = false;

            // End cycle detection
            if (processor.IsCycleDetected)
            {
                if (processor.IsCycleOriginator(this))
                {
                    processor.CycleIsSolved(this);
                }

                errorMessage = ExcelExceptions.CycleException;
                return 0;
            }

            // Evaluate cell from properly evaluated values
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
                            errorMessage = ExcelExceptions.DivisionByZeroException;
                            return 0;
                        }

                        // Division is safe
                        return firstValue / secondValue;
                }
            }

            // Error occured            
            errorMessage = ExcelExceptions.ErrorException;
            return 0;
        }
    }

    #endregion
    
    #region Exceptions

    /// <summary>
    /// Enum class for exception messages
    /// </summary>
    public sealed class ExcelExceptions
    {
        public static string None { get { return String.Empty; } }

        public static string ErrorException { get { return "#ERROR"; } }
        
        public static string DivisionByZeroException { get { return "#DIV0"; } }
    
        public static string CycleException { get { return "#CYCLE"; } }
        
        public static string MissingOperatorException { get { return "#MISSOP"; } }
        
        public static string InvalidFormulaException { get { return "#FORMULA"; } }

        public static string InvalidValueException { get { return "#INVVAL"; } }
    }

    #endregion
}
