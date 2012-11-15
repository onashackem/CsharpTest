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
        private LinkedList<Cell[]> cellRows = new LinkedList<Cell[]>();

        private Dictionary<string, int> sheets = new Dictionary<string, int>();

        public static readonly string EMPTY_CELL_VALUE = "[]";

        public void Process(string inputSheet, string outputSheet)
        {
            // Add input sheet to sheet dictionary
            sheets.Add(inputSheet.Remove(inputSheet.Length - 6), 0);

            using(var reader = new StreamReader(inputSheet))
            using(var writer = new StreamWriter(outputSheet))
            {
                ReadInputSheet(reader);

                EvaluateCells();

                WriteCells(writer);
            }
        }

        public Cell FindCell(string identifier)
        {
            // Invalid cell identifier
            if (!Regex.Match(identifier, @"([^:]+)?[A-Z]+[0-9]+").Success)
            {
                return null;
            }



            return null;
        }

        private void WriteCells(StreamWriter writer)
        {
        }

        private void EvaluateCells()
        {
        }

        private void ReadInputSheet(StreamReader reader)
        {
            string cellLine;
            while((cellLine = reader.ReadLine()) != null)
            {
                var splittedCellLine = cellLine.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);                
                var cells = new Cell[splittedCellLine.Length];
                
                int index = 0;
                foreach(var value in splittedCellLine)
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

                    cells[index++] = cell;
                }

                cellRows.AddLast(cells);
            }
        }
    }

    #region Cells
    abstract class Cell
    {
        /// <summary>
        /// Get identifier of sheet cell is in
        /// </summary>
        protected int Sheet { get; set; }

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
        /// </summary>
        /// <returns>Returns cell value that is printed to the output</returns>
        public virtual string GetPrintValue()
        {
            if (!HasEvaluationSucceeded)
            {
                return errorMessage;
            }

            return EvaluatedValue.ToString();
        }

        /// <summary>
        /// Gets evaluated cell value
        /// </summary>
        public int EvaluatedValue
        {
            get
            {
                // Do not evaluate twice
                if (!isEvaluated)
                {
                    // No matter evaluation fails, cell will be evaluated
                    isEvaluated = true;

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

    class EmptyCell : Cell
    {
        public EmptyCell() 
            : base()
        {
            // This is already evaluated
            isEvaluated = true;
        }

        public override string GetPrintValue()
        {
            return ExcelProcessor.EMPTY_CELL_VALUE;
        }

        protected override int EvaluateValue()
        {
            throw new InvalidOperationException("This method is not supposed to be called!");
        }
    }

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

    class FormulaCell : Cell
    {
        private ExcelProcessor processor;
        private String originalValue;

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

            int operatorPosition = Math.Max(indexOfPlus, Math.Max(indexOfMinus, Math.Max(indexOfMul, indexOfDiv)));
            if (-1 == operatorPosition)
            {
                throw new ExcelMissingOperatorException();
            }

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

            int firstValue = firstCell.EvaluatedValue;
            int secondValue = secondCell.EvaluatedValue;

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
