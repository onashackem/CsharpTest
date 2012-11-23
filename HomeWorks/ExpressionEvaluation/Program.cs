using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExpressionEvaluation
{
    class Program
    {
        static void Main(string[] args)
        {
            try
			{
				string expression = "+ 1 - 2 ~ 3";
				
				int v = new PreorderExpressionEvaluator().EvaluateExpression(new ExpressionParser(expression));
			}
			catch(EvaluationException ex)
			{
				Console.WriteLine(ex.Message);
			} 
        }
    }

    class PreorderExpressionEvaluator : IExpressionEvaluator
    {
        public int EvaluateExpression(IExpressionParser parser)
        {
            Expression expression = BuildExpression(parser);

            return expression.Value;
        }

        /// <summary>
        /// Recursively builds expression
        /// </summary>
        /// <param name="parser">Expression parser</param>
        /// <returns>Returns built expression</returns>
        private Expression BuildExpression(IExpressionParser parser)
        {
            String token = parser.GetNextToken();

            // Any token is expected
            if (token == null)
            {
                throw new FormatException();
            }

            switch (token)
            {
                case "+":
                    return new PlusExpression(BuildExpression(parser), BuildExpression(parser));
                case "-":
                    return new MinusExpression(BuildExpression(parser), BuildExpression(parser));
                case "*":
                    return new MultiplyExpression(BuildExpression(parser), BuildExpression(parser));
                case "/":
                    return new DivideExpression(BuildExpression(parser), BuildExpression(parser));
                case "~":
                    return new UnaryMinusExpression(BuildExpression(parser));

                default:
                    try
                    {
                        checked
                        {
                            int val;
                            if (Int32.TryParse(token, out val))
                            {
                                return new ValueExpression(val);
                            }

                            // Bad format of input
                            throw new FormatException();

                        }
                    }
                    catch (System.OverflowException ex)
                    {
                        throw new OverflowException();
                    }
            }
        }
    }

    class OverFlowChecker
    {
    }

    class ExpressionParser : IExpressionParser
    {
        string[] tokens;
        int nextTokenIndex = 0;

        public ExpressionParser(string expression)
        {
            tokens = expression.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public string GetNextToken()
        {
            // No more tokens
            if (nextTokenIndex == tokens.Length)
            {
                return null;
            }

            // Return next token and increase the counter
            return tokens[nextTokenIndex++];
        }
    }

    #region expressions

    abstract class Expression
    {
        protected int? value;
        public int Value
        {
            get
            {
                if (!value.HasValue)
                {
                    value = Evaluate();
                }

                return value.Value;
            }
        }

        protected abstract int Evaluate();
    }

    abstract class UnaryExpression : Expression
    {
        public Expression Operand { get; protected set; }

        public UnaryExpression(Expression operand)
        {
            Operand = operand;
        }
    }

    abstract class BinaryExpression : Expression
    {
        public Expression LeftOperand { get; protected set; }
        public Expression RightOperand { get; protected set; }

        public BinaryExpression(Expression left, Expression right)
        {
            LeftOperand = left;
            RightOperand = right;
        }
    }

    class UnaryMinusExpression : UnaryExpression
    {
        public UnaryMinusExpression(Expression operand)
            : base(operand)
        {
        }

        protected override int Evaluate()
        {
            return Operand.Value * -1;
        }
    }

    class ValueExpression : UnaryExpression
    {
        public ValueExpression(int operandValue)
            : base(null)
        {
            value = operandValue;
        }

        protected override int Evaluate()
        {
            throw new InvalidOperationException("This method should not be called.");
        }
    }

    class PlusExpression : BinaryExpression
    {
        public PlusExpression(Expression left, Expression right)
            : base(left, right)
        {
        }

        protected override int Evaluate()
        {
            return LeftOperand.Value + RightOperand.Value;
        }
    }

    class MinusExpression : BinaryExpression
    {
        public MinusExpression(Expression left, Expression right)
            : base(left, right)
        {
        }

        protected override int Evaluate()
        {
            return LeftOperand.Value - RightOperand.Value;
        }
    }

    class MultiplyExpression : BinaryExpression
    {
        public MultiplyExpression(Expression left, Expression right)
            : base(left, right)
        {
        }

        protected override int Evaluate()
        {
            return LeftOperand.Value * RightOperand.Value;
        }
    }

    class DivideExpression : BinaryExpression
    {
        public DivideExpression(Expression left, Expression right)
            : base(left, right)
        {
        }

        protected override int Evaluate()
        {
            int left = LeftOperand.Value;
            int right = RightOperand.Value;

            if (right == 0)
            {
                throw new DivideByZeroException();
            }

            return left / right;
        }
    }

    #endregion

    #region expressions

    class EvaluationException : Exception
    {
        public EvaluationException(string message) : base(message) { }
    }

    class OverflowException : EvaluationException
    {
        public OverflowException() : base("Overflow Error") { }
    }

    class DivideByZeroException : EvaluationException
    {
        public DivideByZeroException() : base("Divide Error") { }
    }

    class FormatException : EvaluationException
    {
        public FormatException() : base("Format Error") { }
    }

    #endregion

    #region interfaces
    
    interface IExpressionEvaluator
    {
        int EvaluateExpression(IExpressionParser parser);
    }

    interface IExpressionParser
    {
        string GetNextToken();
    }

    #endregion
}
