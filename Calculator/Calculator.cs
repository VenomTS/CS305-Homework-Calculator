using System.Text.Json;

namespace Calculator;

public class Calculator
{

    private const string FilePath = "History.json";

    private readonly Dictionary<string, double> _history;

    private static readonly Dictionary<char, int> Precedence = new()
    {
        {'u', 3}, // Right Associativity
        {'*', 2}, // Left  Associativity
        {'/', 2}, // Left  Associativity
        {'+', 1}, // Left  Associativity
        {'-', 1}  // Left  Associativity
    };

    public Calculator()
    {
        _history = new Dictionary<string, double>();
        if(!File.Exists(FilePath))
            File.WriteAllText(FilePath, "[]");

        var jsonData = File.ReadAllText(FilePath);
        // if (jsonData == "[]") return;

        try
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, double>>(jsonData);
            _history = data ?? throw new ApplicationException("Error deserializing data from json");
        }
        catch (JsonException ignore)
        {
        }
    }

    public void UseCalculator(int option)
    {
        if (option is < 1 or > 4) return;
        switch (option)
        {
            case 1:
            {
                Console.WriteLine("Enter a mathematical expression to calculate: ");
                var expression = Console.ReadLine();
                
                if (expression is null || !IsValidExpression(expression))
                {
                    Console.WriteLine("ERROR: Invalid Expression");
                    return;
                }
                var modifiedExpression = $"({expression.Replace(" ", "")})";
                
                var result = CalculateBasicExpression(modifiedExpression);
                if (result == null) return;

                AddToHistory(expression, result.Value);
                break;
            }
            case 2:
            {
                Console.WriteLine("Enter an expression to calculate square root: ");
                var expression = Console.ReadLine();
                
                if (expression is null || !IsValidExpression(expression))
                {
                    Console.WriteLine("ERROR: Invalid Expression");
                    break;
                }
                var modifiedExpression = $"({expression.Replace(" ", "")})";
                
                var result = CalculateSqrtExpression(modifiedExpression);
                if (result == null) break;
            
                AddToHistory(expression, result.Value, true);
                break;
            }
            case 3:
                var count = 1;
                if (_history.Count == 0)
                {
                    Console.WriteLine("There is no history to be shown!\n");
                    break;
                }
                Console.WriteLine("Here is the history of calculations: ");
                foreach (var entry in _history)
                {
                    Console.WriteLine($"{count}. {entry.Key} = {entry.Value}");
                    count += 1;
                }

                Console.WriteLine();
                break;
            default:
                Environment.Exit(0);
                break;
        }
    }

    public int? CheckCalculateBasicExpression(string expression)
    {
        if (!IsValidExpression(expression))
        {
            Console.WriteLine("ERROR: Invalid Expression");
            return null;
        }
        var modifiedExpression = $"({expression.Replace(" ", "")})";
        return CalculateBasicExpression(modifiedExpression);
    }
    
    private int? CalculateBasicExpression(string expression)
    {
        var outputStack = new Stack<string>();
        var operatorStack = new Stack<char>();

        char? lastToken = null;
        
        var hasChanged = false;

        var currValue = 0;

        foreach (var token in expression)
        {
            if (token is >= '0' and <= '9')
            {
                currValue = currValue * 10 + (token - '0');
                hasChanged = true;
                lastToken = token;
                continue;
            }

            if (hasChanged)
            {
                hasChanged = false;
                outputStack.Push(currValue.ToString());
                currValue = 0;
            }

            if (token == '-' && lastToken is null or '(' or '+' or '-' or '*' or '/') // We should treat it as a unary operator
            {
                operatorStack.Push('u');
                lastToken = 'u';
                continue;
            }

            if (token == '(')
            {
                operatorStack.Push('(');
                lastToken = '(';
                continue;
            }

            if (token == ')')
            {
                while (operatorStack.Count > 0 && operatorStack.Peek() != '(')
                {
                    outputStack.Push(operatorStack.Pop().ToString());
                }

                if (operatorStack.Pop() != '(')
                {
                    Console.WriteLine("ERROR: Invalid Expression");
                    return null;
                }
                // if (operatorStack.Pop() != '(') throw new InvalidExpressionException("Invalid Expression");
                continue;
            }

            while (operatorStack.Count > 0 && operatorStack.Peek() != '(' &&
                   (Precedence[operatorStack.Peek()] > Precedence[token] || Precedence[operatorStack.Peek()] == Precedence[token]))
            {
                outputStack.Push(operatorStack.Pop().ToString());
            }

            operatorStack.Push(token);
            lastToken = token;
        }

        var outputString = outputStack.Reverse().Aggregate("", (current, item) => current + $"{item}S");
        outputString = outputString.Remove(outputString.Length - 1).Replace("S", " ");
        
        // Debug
        // Console.WriteLine(outputString);

        return ParseReversePolishNotation(outputString.Split(' ').ToList());
    }

    private double? CalculateSqrtExpression(string expression)
    {
        var result = CalculateBasicExpression(expression);
        if (result < 0)
        {
            Console.WriteLine("ERROR: Cannot calculate square root of a negative number!");
            return null;
        }
        if (result == null) return null;
        return Math.Sqrt(result.Value * 1.0d);
    }

    private static int? ParseReversePolishNotation(List<string> expression)
    {

        var operators = new[] { "+", "-", "*", "/", "u" };
        
        for (var i = 0; i < expression.Count; i++)
        {
            if (!operators.Contains(expression[i])) continue;
            switch (expression[i])
            {
                case "u":
                {
                    if (i - 1 < 0)
                    {
                        Console.WriteLine("ERROR: Unary operator (-) used without having a value associated with it");
                        return null;
                    }
                    var number = int.Parse(expression[i - 1]) * -1;
                    expression[i - 1] = number.ToString();
                    expression[i] = "DELETE";
                    i -= 1;
                    break;
                }
                case "+":
                {
                    if(i - 2 < 0)
                    {
                        Console.WriteLine("ERROR: Binary operator (+) used without having values associated with it");
                        return null;
                    }
                    var firstNumber = int.Parse(expression[i - 2]);
                    var secondNumber = int.Parse(expression[i - 1]);
                    expression[i] = expression[i - 1] = expression[i - 2] = "DELETE";
                    expression[i] = (firstNumber + secondNumber).ToString();
                    i -= 2;
                    break;
                }
                case "-":
                {
                    if(i - 2 < 0)
                    {
                        Console.WriteLine("ERROR: Binary operator (-) used without having values associated with it");
                        return null;
                    }
                    var firstNumber = int.Parse(expression[i - 2]);
                    var secondNumber = int.Parse(expression[i - 1]);
                    expression[i] = expression[i - 1] = expression[i - 2] = "DELETE";
                    expression[i] = (firstNumber - secondNumber).ToString();
                    i -= 2;
                    break;
                }
                case "*":
                {
                    if(i - 2 < 0)
                    {
                        Console.WriteLine("ERROR: Binary operator (*) used without having values associated with it");
                        return null;
                    }
                    var firstNumber = int.Parse(expression[i - 2]);
                    var secondNumber = int.Parse(expression[i - 1]);
                    expression[i] = expression[i - 1] = expression[i - 2] = "DELETE";
                    expression[i] = (firstNumber * secondNumber).ToString();
                    i -= 2;
                    break;
                }
                case "/":
                {
                    if(i - 2 < 0)
                    {
                        Console.WriteLine("ERROR: Binary operator (/) used without having values associated with it");
                        return null;
                    }
                    var firstNumber = int.Parse(expression[i - 2]);
                    var secondNumber = int.Parse(expression[i - 1]);

                    if (secondNumber == 0)
                    {
                        Console.WriteLine();
                    }
                    
                    expression[i] = expression[i - 1] = expression[i - 2] = "DELETE";
                    expression[i] = (firstNumber / secondNumber).ToString();
                    i -= 2;
                    break;
                }
                default:
                    Console.WriteLine($"ERROR: Unknown Operator ({expression[i]})");
                    return null;
            }

            expression.RemoveAll((x) => x == "DELETE");
        }
        
        return int.Parse(expression[0]);
    }

    private static bool IsValidExpression(string expression)
    {
        var validTokens = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '+', '-', '*', '/', '(', ')' };
        expression = $"({expression.Replace(" ", "")})";
        var bracketsCount = 0;
        foreach (var token in expression)
        {
            if (!validTokens.Contains(token)) return false;
            switch (token)
            {
                case '(':
                    bracketsCount += 1;
                    break;
                case ')':
                    bracketsCount -= 1;
                    if (bracketsCount < 0) return false;
                    break;
            }
        }
        return bracketsCount == 0;
    }

    private void AddToHistory(string expression, double result, bool isSqrt = false)
    {
        if (isSqrt) expression = "√(" + expression + ")";
        _history.Add(expression, result);
        var data = JsonSerializer.Serialize(_history);
        File.WriteAllText(FilePath, data);

        if (isSqrt) Console.WriteLine($"Square Root: {result}");
        else Console.WriteLine($"Result: {result}");
        Console.WriteLine();
    }
}