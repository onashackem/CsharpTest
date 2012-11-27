using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExpressionEvaluation
{
    class Evaluation
    {
        static void Main(string[] args)
        {
            try
			{   
                // Read, parse and evaluate expression
                var evaluator =  new PreorderExpressionEvaluator();
                int result = evaluator.EvaluateExpression(new ExpressionParser(Console.ReadLine()));
                
                Console.WriteLine(result);
			}
            catch (System.OverflowException)
            {
                // Overflow exception during evaluation
                Console.WriteLine(new OverflowException().Message);
            }
			catch(EvaluationException ex)
			{
                // Some other exception
				Console.WriteLine(ex.Message);
			} 
        }
    }

    /// <summary>
    /// Evaluator works with preorder formated expression
    /// </summary>
    class PreorderExpressionEvaluator : IExpressionEvaluator
    {
        /// <summary>
        /// Builds expression from tokens parsed by parser.
        /// </summary>
        /// <param name="parser">Parser to get tokens from</param>
        /// <returns>Returns evaluated expression</returns>
        public int EvaluateExpression(IExpressionParser parser)
        {
            // Build expression
            Expression expression = BuildExpression(parser);

            // Some tokens are not processed
            if (parser.GetNextToken() != null)
            {
                throw new FormatException();
            }

            return expression.Value;
        }

        /// <summary>
        /// Recursively builds expression
        /// </summary>
        /// <param name="parser">Expression parser to get tokens from</param>
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

                default: // Number
                    int val;
                    if (Int32.TryParse(token, out val))
                    {
                        return new ValueExpression(val);
                    }

                    // Bad format of input
                    throw new FormatException();
            }
        }
    }

    /// <summary>
    /// Parses expressions int tokens - operators and operands
    /// </summary>
    class ExpressionParser : IExpressionParser
    {
        string[] tokens;
        int nextTokenIndex = 0;

        /// <summary>
        /// Accepts expression to parse
        /// </summary>
        /// <param name="input">expression to parse</param>
        public ExpressionParser(string input)
        {
            tokens = input.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Get next parsed token in proper order.
        /// </summary>
        /// <param name="input">Expression to parse</param>
        /// <returns>Returns next token</returns>
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

    /// <summary>
    /// Basic IExpression implementation. Value is evaluated and remembered for further evaluation.
    /// </summary>
    abstract class Expression
    {
        /// <summary>
        /// If defined, contains evaluated value
        /// </summary>
        protected int? value;

        /// <summary>
        /// Gets value of the expression. Once value is evaluated, is cached.
        /// </summary>
        public int Value
        {
            get
            {
                // Evaluate only if not evaluated yet
                if (!value.HasValue)
                {
                    value = Evaluate();
                }

                return value.Value;
            }
        }

        /// <summary>
        /// Calculates value of this expression.
        /// </summary>
        /// <returns>Returns calculated value</returns>
        protected abstract int Evaluate();
    }

    /// <summary>
    /// Basic implementation of expression with one operand. Stores the one operand.
    /// </summary>
    abstract class UnaryExpression : Expression
    {
        /// <summary>
        /// Gets the only operand of this expression
        /// </summary>
        public Expression Operand { get; protected set; }

        /// <summary>
        /// .ctor with the operand
        /// </summary>
        /// <param name="operand">The only operand of this expression</param>
        public UnaryExpression(Expression operand)
        {
            Operand = operand;
        }
    }

    /// <summary>
    /// Basic implementation of expression with two operands. Stores both of them.
    /// </summary>
    abstract class BinaryExpression : Expression
    {
        /// <summary>
        /// Gets the first operand of the expression
        /// </summary>
        public Expression LeftOperand { get; protected set; }

        /// <summary>
        /// Gets the second operand of the expression
        /// </summary>
        public Expression RightOperand { get; protected set; }

        /// <summary>
        /// .ctor with both operands
        /// </summary>
        /// <param name="left">The left (first) operand</param>
        /// <param name="right">The right (second) operand</param>
        public BinaryExpression(Expression left, Expression right)
        {
            LeftOperand = left;
            RightOperand = right;
        }
    }

    /// <summary>
    /// Implementaion of unary minus.
    /// </summary>
    class UnaryMinusExpression : UnaryExpression
    {
        public UnaryMinusExpression(Expression operand)
            : base(operand)
        {
        }

        protected override int Evaluate()
        {
            checked { return Operand.Value * -1; };
        }
    }

    /// <summary>
    /// Implementation of simple value. Is evaluated in the moment of declaration.
    /// </summary>
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

    /// <summary>
    /// Implementation of expression: left + right
    /// </summary>
    class PlusExpression : BinaryExpression
    {
        public PlusExpression(Expression left, Expression right)
            : base(left, right)
        {
        }

        protected override int Evaluate()
        {
            checked { return LeftOperand.Value + RightOperand.Value; }
        }
    }

    /// <summary>
    /// Implementation of expression: left - right
    /// </summary>
    class MinusExpression : BinaryExpression
    {
        public MinusExpression(Expression left, Expression right)
            : base(left, right)
        {
        }

        protected override int Evaluate()
        {
            checked { return LeftOperand.Value - RightOperand.Value; }
        }
    }

    /// <summary>
    /// Implementation of expression: left * right
    /// </summary>
    class MultiplyExpression : BinaryExpression
    {
        public MultiplyExpression(Expression left, Expression right)
            : base(left, right)
        {
        }

        protected override int Evaluate()
        {
            checked { return LeftOperand.Value * RightOperand.Value; }
        }
    }

    /// <summary>
    /// Implementation of expression: left / right . Checks for division by zero.
    /// </summary>
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

            checked { return left / right; };
        }
    }

    #endregion

    #region exceptions

    /// <summary>
    /// Common parent for exceptions that can be thrown during evaluation
    /// </summary>
    abstract class EvaluationException : Exception
    {
        public EvaluationException(string message) : base(message) { }
    }

    /// <summary>
    /// Indicates that an arithmetic overflow occured.
    /// </summary>
    class OverflowException : EvaluationException
    {
        public OverflowException() : base("Overflow Error") { }
    }

    /// <summary>
    /// Indicates that a division by zero occured.
    /// </summary>
    class DivideByZeroException : EvaluationException
    {
        public DivideByZeroException() : base("Divide Error") { }
    }

    /// <summary>
    /// Indicated that input expression was in inproper format - syntax error, invalid operands, ...
    /// </summary>
    class FormatException : EvaluationException
    {
        public FormatException() : base("Format Error") { }
    }

    #endregion

    #region interfaces

    /// <summary>
    /// Defines interface for all expressions.
    /// </summary>
    interface IExpression
    {
        int Value { get; }
    }
    
    /// <summary>
    /// Defines interface for expression evaluators.
    /// </summary>
    interface IExpressionEvaluator
    {
        int EvaluateExpression(IExpressionParser parser);
    }

    /// <summary>
    /// Defines interface for expression parsers.
    /// </summary>
    interface IExpressionParser
    {
        string GetNextToken();
    }

    #endregion
}
