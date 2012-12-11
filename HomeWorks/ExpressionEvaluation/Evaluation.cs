using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.IO;

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

            // Set culture explicitely - because of CodEx ...
            var enUS = new System.Globalization.CultureInfo("en-US");
            /*

            Console.WriteLine(Double.MaxValue);
            Console.WriteLine((1000.0).ToString("######.00000"));
            Console.WriteLine(Double.NaN.ToString("N5", CultureInfo.CreateSpecificCulture("en-US"))));
            Console.WriteLine(String.Format("01: {0:N5}", Double.NegativeInfinity.ToString("N5", CultureInfo.CreateSpecificCulture("en-US"))));
            Console.WriteLine(String.Format("02: {0:N5}", Double.PositiveInfinity.ToString()));
            Console.WriteLine(String.Format("03: {0:N5}", Double.NegativeInfinity.ToString()));
            Console.WriteLine(String.Format("04: {0:N5}", 10.0 / 0.0));
            Console.WriteLine(String.Format("05: {0:N5}", 10.0 / -0.0));
            Console.WriteLine(String.Format("06: {0:N5}", Double.NegativeInfinity / Double.PositiveInfinity));
            Console.WriteLine(String.Format("07: {0:N5}", Double.NegativeInfinity / Double.NaN));
            Console.WriteLine(String.Format("08: {0:N5}", Double.NaN / Double.NegativeInfinity));
            Console.WriteLine(String.Format("09: {0:N5}", 1.0 / Double.PositiveInfinity));
            Console.WriteLine(String.Format("10: {0:N5}", Double.NegativeInfinity / -5.5));
            Console.WriteLine(String.Format("11: {0:N5}", Double.NaN / -10.0));
            Console.WriteLine(String.Format("12: {0:N5}", -10.0 / Double.NaN));
            Console.WriteLine(String.Format("13: {0:N5}", -1.0 / Double.PositiveInfinity));
            Console.WriteLine(String.Format("14: {0:N5}", 1.0 / Double.NegativeInfinity));

            Console.WriteLine();
            */

            // Read commands until the end
            string command;
            while ((command = Console.ReadLine()) != null && !command.Equals("end"))
            {
                // No command - skip
                if (command.Length == 0)
                {
                    continue;
                }

                switch (command[0])
                {
                    // Evaluate expression in integer mode
                    case 'i':
                        if (command.Length > 1)
                        {
                            // Incorrect command
                            Console.WriteLine(new FormatException().Message);
                            break;
                        }

                        if (expression == null)
                        {
                            Console.WriteLine(new ExpressionMissing().Message);
                            break;
                        }

                        EvaluateIntegerExpression(intVisitor, expression);
                        break;

                    // Evaluate expression in a floating point mode
                    case 'd':
                        if (command.Length > 1)
                        {
                            // Incorrect command
                            Console.WriteLine(new FormatException().Message);
                            break;
                        }

                        if (expression == null)
                        {
                            Console.WriteLine(new ExpressionMissing().Message);
                            break;
                        }

                        Console.WriteLine(expression.AcceptVisitor(doubleVisitor).ToString("#.00000", enUS));
                        break;

                    // Evaluate expression in a floating point mode
                    case 'p':
                        if (command.Length > 1)
                        {
                            // Incorrect command
                            Console.WriteLine(new FormatException().Message);
                            break;
                        }

                        if (expression == null)
                        {
                            Console.WriteLine(new ExpressionMissing().Message);
                            break;
                        }

                        expression.AcceptVisitor(new MaximumParenthesesVisitor());
                        Console.WriteLine();
                        break;

                    // Evaluate expression in a floating point mode
                    case 'P':
                        if (command.Length > 1)
                        {
                            // Incorrect command
                            Console.WriteLine(new FormatException().Message);
                            break;
                        }

                        if (expression == null)
                        {
                            Console.WriteLine(new ExpressionMissing().Message);
                            break;
                        }
                        
                        expression.AcceptVisitor(new MinimumParenthesesVisitor());
                        Console.WriteLine();
                        break;

                    default:
                        // Forget the expression
                        expression = null;

                        if (command[0] == '=')
                        {
                            // Read, parse and evaluate expression
                            var parser = new ExpressionParser(command.Substring(1));
                            try
                            {
                                expression = builder.BuildExpression(parser);

                                // Expression is parsed correctly
                                if (expression != null)
                                {
                                    break;
                                }
                            }
                            catch (FormatException)
                            {
                                // Do nothing, format error is reported via next line
                            }
                        }

                        // Incorrect command or incorrect expression
                        Console.WriteLine(new FormatException().Message);
                        break;
                }                
            }
        }

        private static void EvaluateIntegerExpression(IntExpressionVisitor intVisitor, IExpression expression)
        {
            try
            {
                Console.WriteLine(expression.AcceptVisitor(intVisitor));
            }
            catch (System.OverflowException)
            {
                // Overflow exception during evaluation
                Console.WriteLine(new OverflowException().Message);
            }
            catch (DivideByZeroException ex)
            {
                // Some other exception
                Console.WriteLine(ex.Message);
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
                return null;
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

    class MinimumParenthesesVisitor : IExpressionVisitor<bool>
    {
        private TextWriter output = Console.Out;
        //= - 1 - 2 * + 3 4 * + 5 6 - 7 + 8 - 8 ~ 1

        public bool Visit(ValueExpression expression)
        {
            output.Write(expression.Value);
            return false;
        }

        public bool Visit(UnaryMinusExpression expression)
        {
            WrapInBrackets(expression);
            return true;
        }

        public bool Visit(PlusExpression expression)
        {
            WrapInBrackets(expression, '+');
            return true;
        }

        public bool Visit(MinusExpression expression)
        {
            WrapInBrackets(expression, '-');
            return true;
        }

        public bool Visit(MultiplyExpression expression)
        {
            WrapInBrackets(expression, '*');
            return true;
        }

        public bool Visit(DivideExpression expression)
        {
            WrapInBrackets(expression, '/');
            return true;
        }

        private void WrapInBrackets(BinaryExpression expression, char oper)
        {
            var leftOperand = expression.LeftOperand;
            var rightOperand = expression.RightOperand;

            if (leftOperand.OperatorPriority < expression.OperatorPriority)
            {
                output.Write("(");
                leftOperand.AcceptVisitor(this);
                output.Write(")");
            }
            else
            {
                leftOperand.AcceptVisitor(this);
            }

            output.Write(oper);

            if (rightOperand.OperatorPriority < expression.OperatorPriority)
            {
                output.Write("(");
                rightOperand.AcceptVisitor(this);
                output.Write(")");
            }
            else
            {
                rightOperand.AcceptVisitor(this);
            }
        }

        private void WrapInBrackets(UnaryMinusExpression expression)
        {
            output.Write("(");
            output.Write("-");
            expression.Operand.AcceptVisitor(this);
            output.Write(")");
        }
    }

    class MaximumParenthesesVisitor : IExpressionVisitor<bool>
    {
        private TextWriter output = Console.Out;

        public bool Visit(ValueExpression expression)
        {
            output.Write(expression.Value);
            return false;
        }

        public bool Visit(UnaryMinusExpression expression)
        {
            WrapInBrackets(expression);
            return true;
        }

        public bool Visit(PlusExpression expression)
        {
            WrapInBrackets(expression, '+');
            return true;
        }

        public bool Visit(MinusExpression expression)
        {
            WrapInBrackets(expression, '-');
            return true;
        }

        public bool Visit(MultiplyExpression expression)
        {
            WrapInBrackets(expression, '*');
            return true;
        }

        public bool Visit(DivideExpression expression)
        {
            WrapInBrackets(expression, '/');
            return true;
        }

        private void WrapInBrackets(BinaryExpression expression, char oper)
        {
            output.Write("(");
            expression.LeftOperand.AcceptVisitor(this);
            output.Write(oper);
            expression.RightOperand.AcceptVisitor(this);
            output.Write(")");
        }

        private void WrapInBrackets(UnaryMinusExpression expression)
        {
            output.Write("(");
            output.Write("-");
            expression.Operand.AcceptVisitor(this);
            output.Write(")");
        }
    }

    class IntExpressionVisitor : IExpressionVisitor<int>
    {
        public int Visit(ValueExpression expression)
        {
            return expression.Value;
        }

        public int Visit(UnaryMinusExpression expression)
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
            int rightValue = expression.RightOperand.AcceptVisitor(this);

            if (rightValue == 0)
            {
                throw new DivideByZeroException();
            }

            checked { return expression.LeftOperand.AcceptVisitor(this) / rightValue; }
        }
    }

    class DoubleExpressionVisitor : IExpressionVisitor<double>
    {
        public double Visit(ValueExpression expression)
        {
            return expression.Value * 1.0;
        }

        public double Visit(UnaryMinusExpression expression)
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

        public int OperatorPriority { get; protected set; }

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
            OperatorPriority = 1;
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
            OperatorPriority = 1000;
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
            OperatorPriority = 2;
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
            OperatorPriority = 3;
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
            OperatorPriority = 10;
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
            OperatorPriority = 11;
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

        T Visit(UnaryMinusExpression expression);

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

        int OperatorPriority { get; }
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
