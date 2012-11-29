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
            var builder = new PreorderExpressionBuilder();
            var intVisitor = new IntExpressionVisitor();
            var doubleVisitor = new DoubleExpressionVisitor();
            IExpression expression = null;

            // Read commands until the end
            string command;
            while ((command = Console.ReadLine()) != null && !command.Equals("end", StringComparison.InvariantCultureIgnoreCase))
            {
                try
                {
                    if (command.Length > 0)
                    {
                        switch (command[0])
                        {
                            // Evaluate expression in integer domain
                            case 'i':
                                if (expression == null)
                                {
                                    Console.WriteLine(new ExpressionMissing().Message);
                                    break;
                                }

                                Console.WriteLine(expression.AcceptVisitor(intVisitor));
                                break;

                            // Evaluate expression in a floating point
                            case 'd':
                                if (expression == null)
                                {
                                    Console.WriteLine(new ExpressionMissing().Message);
                                    break;
                                }

                                Console.WriteLine( String.Format("{0:N5}", expression.AcceptVisitor(doubleVisitor)));
                                break;

                            default:

                                if (command[0] == '=')
                                {
                                    // Read, parse and evaluate expression
                                    var parser = new ExpressionParser(command.Substring(1));
                                    expression = builder.BuildExpression(parser);
                                    break;
                                }
                                else
                                {
                                    Console.WriteLine(new FormatException().Message);
                                }
                                break;
                        }
                    }
                }
                catch (System.OverflowException)
                {
                    // Overflow exception during evaluation
                    Console.WriteLine(new OverflowException().Message);
                }
                catch (EvaluationException ex)
                {
                    // Some other exception
                    Console.WriteLine(ex.Message);
                }
            }

        }
    }

    /*
    /// <summary>
    /// Evaluator works with preorder formated expression
    /// </summary>
    class PreorderExpressionEvaluator : IExpressionEvaluator
    {
        /// <summary>
        /// Builds expression from tokens parsed by parser and evaluates it.
        /// </summary>
        /// <param name="builder">Builder that uses parser to build an expression</param>
        /// <param name="parser">Parser to get tokens from</param>
        /// <returns>Returns evaluated expression</returns>
        public int EvaluateExpression(IExpressionBuider builder, IExpressionParser parser)
        {
            return builder.BuildExpression(parser).Value;
        }
    }
     */

    class PreorderExpressionBuilder : IExpressionBuider
    {
        /// <summary>
        /// Builds expression
        /// </summary>
        /// <param name="parser">Expression parser to get tokens from</param>
        /// <returns>Returns built expression</returns>
        public IExpression BuildExpression(IExpressionParser parser)
        {
            var expression = BuildExpressionNode(parser);

            // Some tokens are not processed
            if (parser.GetNextToken() != null)
            {
                throw new FormatException();
            }

            return expression;
        }

        private Expression BuildExpressionNode(IExpressionParser parser)
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
                    return new PlusExpression(BuildExpressionNode(parser), BuildExpressionNode(parser));
                case "-":
                    return new MinusExpression(BuildExpressionNode(parser), BuildExpressionNode(parser));
                case "*":
                    return new MultiplyExpression(BuildExpressionNode(parser), BuildExpressionNode(parser));
                case "/":
                    return new DivideExpression(BuildExpressionNode(parser), BuildExpressionNode(parser));
                case "~":
                    return new UnaryMinusExpression(BuildExpressionNode(parser));

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

    #region Visitors

    class IntExpressionVisitor : IExpressionVisitor<int>
    {
        public int Visit(ValueExpression expression)
        {
            return expression.Value;
        }

        public int Visit(UnaryExpression expression)
        {
            checked { return expression.Operand.AcceptVisitor(this) * -1; }
        }

        public int Visit(PlusExpression expression)
        {
            checked { return expression.LeftOperand.AcceptVisitor(this) + expression.RightOperand.AcceptVisitor(this); }
        }

        public int Visit(MinusExpression expression)
        {
            checked { return expression.LeftOperand.AcceptVisitor(this) - expression.RightOperand.AcceptVisitor(this); }
        }

        public int Visit(MultiplyExpression expression)
        {
            checked { return expression.LeftOperand.AcceptVisitor(this) * expression.RightOperand.AcceptVisitor(this); }
        }

        public int Visit(DivideExpression expression)
        {
            checked { return expression.LeftOperand.AcceptVisitor(this) / expression.RightOperand.AcceptVisitor(this); }
        }
    }

    class DoubleExpressionVisitor : IExpressionVisitor<double>
    {
        public double Visit(ValueExpression expression)
        {
            return expression.Value * 1.0;
        }

        public double Visit(UnaryExpression expression)
        {
            return expression.Operand.AcceptVisitor(this) * -1.0;
        }

        public double Visit(PlusExpression expression)
        {
            return expression.LeftOperand.AcceptVisitor(this) + expression.RightOperand.AcceptVisitor(this);
        }

        public double Visit(MinusExpression expression)
        {
            return expression.LeftOperand.AcceptVisitor(this) - expression.RightOperand.AcceptVisitor(this);
        }

        public double Visit(MultiplyExpression expression)
        {
            return expression.LeftOperand.AcceptVisitor(this) * expression.RightOperand.AcceptVisitor(this);
        }

        public double Visit(DivideExpression expression)
        {
            return expression.LeftOperand.AcceptVisitor(this) / expression.RightOperand.AcceptVisitor(this);
        }
    }
    

    #endregion

    #region expressions

    /// <summary>
    /// Basic IExpression implementation. Value is evaluated and remembered for further evaluation.
    /// </summary>
    abstract class Expression: IExpression
    {
        public abstract T AcceptVisitor<T>(IExpressionVisitor<T> visitor);

        /*
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
         * */
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

        public override T AcceptVisitor<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }

        /*
        protected override int Evaluate()
        {
            checked { return Operand.Value * -1; };
        }
         * */
    }

    /// <summary>
    /// Implementation of simple value. Is evaluated in the moment of declaration.
    /// </summary>
    class ValueExpression : UnaryExpression
    {
        public int Value { get; private set; }

        public ValueExpression(int operandValue)
            : base(null)
        {
            Value = operandValue;
        }

        public override T AcceptVisitor<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }

        /*
        protected override int Evaluate()
        {
            throw new InvalidOperationException("This method should not be called.");
        }
         * */
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

        public override T AcceptVisitor<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }

        /*
        protected override int Evaluate()
        {
            checked { return LeftOperand.Value + RightOperand.Value; }
        }
         * */
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

        public override T AcceptVisitor<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }

        /*
        protected override int Evaluate()
        {
            checked { return LeftOperand.Value - RightOperand.Value; }
        }
         * */
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

        public override T AcceptVisitor<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }

        /*
        protected override int Evaluate()
        {
            checked { return LeftOperand.Value * RightOperand.Value; }
        }
         * */
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

        public override T AcceptVisitor<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }

        /*
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
         */
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

    /// <summary>
    /// Indicated that an expression to evaluate is missing
    /// </summary>
    class ExpressionMissing : EvaluationException
    {
        public ExpressionMissing() : base("Expression Missing") { }
    }

    #endregion

    #region interfaces

    interface IExpressionVisitor<T>
    {
        T Visit(ValueExpression expression);

        T Visit(UnaryExpression expression);

        T Visit(PlusExpression expression);

        T Visit(MinusExpression expression);

        T Visit(MultiplyExpression expression);

        T Visit(DivideExpression expression);
    }

    /// <summary>
    /// Defines interface for all expressions.
    /// </summary>
    interface IExpression
    {
        //double Value { get; }

        /// <summary>
        /// Accepts invitation of visitor to visit it;
        /// </summary>
        /// <typeparam name="T">Return type of visitor</typeparam>
        /// <param name="visitor">Visitor to visit</param>
        /// <returns>Returns result of visit</returns>
        T AcceptVisitor<T>(IExpressionVisitor<T> visitor);
    }

    interface IExpressionBuider
    {
        IExpression BuildExpression(IExpressionParser parser);
    }
    
    /// <summary>
    /// Defines interface for expression evaluators.
    /// </summary>
    interface IExpressionEvaluator
    {
        int EvaluateExpression(IExpressionBuider builder, IExpressionParser parser);
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
