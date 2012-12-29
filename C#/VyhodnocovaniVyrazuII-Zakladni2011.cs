using System;
using System.Collections.Generic;

namespace ExpressionEvaluator {

	interface IExpressionVisitor<R> {
		R Visit(ConstantExpression ex);
		R Visit(PlusExpression ex);
		R Visit(MinusExpression ex);
		R Visit(MultiplyExpression ex);
		R Visit(DivideExpression ex);
		R Visit(UnaryMinusExpression ex);
	}

	sealed class NoResult {
		public static readonly NoResult Default = null;

		private NoResult() { }
	}

	abstract class Expression {
		public static Expression ParsePrefixExpression(string exprString) {
			string[] tokens = exprString.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

			Expression result = null;
			Stack<OperatorExpression> unresolved = new Stack<OperatorExpression>();
			foreach (string token in tokens) {
				if (result != null) {
					// We correctly parsed the whole tree, but there was at least one more unprocessed token left.
					// This implies incorrect input, thus return null.

					return null;
				}

				switch (token) {
					case "+":
						unresolved.Push(new PlusExpression());
						break;

					case "-":
						unresolved.Push(new MinusExpression());
						break;

					case "*":
						unresolved.Push(new MultiplyExpression());
						break;

					case "/":
						unresolved.Push(new DivideExpression());
						break;

					case "~":
						unresolved.Push(new UnaryMinusExpression());
						break;

					default:
						int value;
						if (!int.TryParse(token, out value)) {
							return null;	// Invalid token format
						}

						Expression expr = new ConstantExpression(value);
						while (unresolved.Count > 0) {
							OperatorExpression oper = unresolved.Peek();
							if (oper.AddOperand(expr)) {
								unresolved.Pop();
								expr = oper;
							} else {
								expr = null;
								break;
							}
						}

						if (expr != null) {
							result = expr;
						}

						break;
				}
			}

			return result;
		}

		public abstract int Evaluate();

		public abstract R Accept<R>(IExpressionVisitor<R> visitor);
	}

	abstract class ValueExpression : Expression {
		public abstract int Value {
			get;
		}

		public sealed override int Evaluate() {
			return Value;
		}
	}

	sealed class ConstantExpression : ValueExpression {
		private int value;

		public ConstantExpression(int value) {
			this.value = value;
		}

		public override int Value {
			get { return this.value; }
		}

		public override R Accept<R>(IExpressionVisitor<R> visitor) {
			return visitor.Visit(this);
		}
	}

	abstract class OperatorExpression : Expression {
		public abstract bool AddOperand(Expression op);
	}

	abstract class UnaryExpression : OperatorExpression {
		protected Expression op;

		public Expression Op {
			get { return op; }
			set { op = value; }
		}

		public override bool AddOperand(Expression op) {
			if (this.op == null) {
				this.op = op;
			}
			return true;
		}

		public sealed override int Evaluate() {
			return Evaluate(op.Evaluate());
		}

		protected abstract int Evaluate(int opValue);
	}

	abstract class BinaryExpression : OperatorExpression {
		protected Expression op0, op1;

		public Expression Op0 {
			get { return op0; }
			set { op0 = value; }
		}

		public Expression Op1 {
			get { return op1; }
			set { op1 = value; }
		}

		public override bool AddOperand(Expression op) {
			if (op0 == null) {
				op0 = op;
				return false;
			} else if (op1 == null) {
				op1 = op;
			}
			return true;
		}

		public sealed override int Evaluate() {
			return Evaluate(op0.Evaluate(), op1.Evaluate());
		}

		protected abstract int Evaluate(int op0Value, int op1Value);
	}

	sealed class PlusExpression : BinaryExpression {
		protected override int Evaluate(int op0Value, int op1Value) {
			return checked(op0Value + op1Value);
		}

		public override R Accept<R>(IExpressionVisitor<R> visitor) {
			return visitor.Visit(this);
		}
	}

	sealed class MinusExpression : BinaryExpression {
		protected override int Evaluate(int op0Value, int op1Value) {
			return checked(op0Value - op1Value);
		}

		public override R Accept<R>(IExpressionVisitor<R> visitor) {
			return visitor.Visit(this);
		}
	}

	sealed class MultiplyExpression : BinaryExpression {
		protected override int Evaluate(int op0Value, int op1Value) {
			return checked(op0Value * op1Value);
		}

		public override R Accept<R>(IExpressionVisitor<R> visitor) {
			return visitor.Visit(this);
		}
	}

	sealed class DivideExpression : BinaryExpression {
		protected override int Evaluate(int op0Value, int op1Value) {
			return (op0Value / op1Value);
		}

		public override R Accept<R>(IExpressionVisitor<R> visitor) {
			return visitor.Visit(this);
		}
	}

	sealed class UnaryMinusExpression : UnaryExpression {
		protected override int Evaluate(int opValue) {
			return checked(-opValue);
		}

		public override R Accept<R>(IExpressionVisitor<R> visitor) {
			return visitor.Visit(this);
		}
	}

	class DoubleEvaluatingVisitor : IExpressionVisitor<double> {
		public double Visit(ConstantExpression ex) {
			return ex.Value;
		}

		public double Visit(PlusExpression ex) {
			return ex.Op0.Accept(this) + ex.Op1.Accept(this);
		}

		public double Visit(MinusExpression ex) {
			return ex.Op0.Accept(this) - ex.Op1.Accept(this);
		}

		public double Visit(MultiplyExpression ex) {
			return ex.Op0.Accept(this) * ex.Op1.Accept(this);
		}

		public double Visit(DivideExpression ex) {
			return ex.Op0.Accept(this) / ex.Op1.Accept(this);
		}

		public double Visit(UnaryMinusExpression ex) {
			return -ex.Op.Accept(this);
		}

		public double Evaluate(Expression ex) {
			return ex.Accept(this);
		}
	}


	class Program {
		static void Main(string[] args) {
			Expression expr = null;

			while (true) {
				string line = Console.ReadLine();
				if (line == null || line == "end") {
					break;
				} else if (line == "") {
					continue;
				}

				try {
					switch (line[0]) {
						case '=':
							expr = null;

							if (line.Length >= 3) {
								expr = Expression.ParsePrefixExpression(line.Substring(2));
							}

							if (expr == null) {
								Console.WriteLine("Format Error");
							}
							break;

						case 'i':
							if (line.Length != 1) {
								Console.WriteLine("Format Error");
							} else {
								Console.WriteLine(expr.Evaluate().ToString());
							}
							break;

						case 'd':
							if (line.Length != 1) {
								Console.WriteLine("Format Error");
							} else {
								Console.WriteLine(new DoubleEvaluatingVisitor().Evaluate(expr).ToString("f05"));
							}
							break;

						default:
							Console.WriteLine("Format Error");
							break;
					}
				} catch (DivideByZeroException) {
					Console.WriteLine("Divide Error");
				} catch (OverflowException) {
					Console.WriteLine("Overflow Error");
				} catch (NullReferenceException) {
					Console.WriteLine("Expression Missing");
				}
			}
		}
	}
}
