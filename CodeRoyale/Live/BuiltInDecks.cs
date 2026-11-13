namespace CodeRoyale.Live;

/// <summary>
/// The built-in question decks every user can host, as the requirements call for.
/// These live in code for now; M2 replaces this with decks loaded from question_sets
/// so users can author their own.
/// </summary>
public static class BuiltInDecks
{
    public static IReadOnlyList<QuestionDeck> All { get; } = new[]
    {
        CSharpBasics(),
        JavaScriptQuirks(),
        BigO()
    };

    public static QuestionDeck? ById(string? id) =>
        All.FirstOrDefault(d => string.Equals(d.Id, id, StringComparison.OrdinalIgnoreCase));

    private static QuestionView Q(int index, string text, int correct, params string[] options) => new()
    {
        Index = index,
        Text = text,
        Options = options.Select((t, i) => new AnswerOption
        {
            Index = i,
            Text = t,
            IsCorrect = i == correct
        }).ToList()
    };

    private static QuestionDeck CSharpBasics() => new()
    {
        Id = "csharp-basics",
        Name = "C# Basics",
        Description = "Types, keywords and the things every C# dev trips over once.",
        SecondsPerQuestion = 20,
        Questions = new List<QuestionView>
        {
            Q(0, "What does the 'var' keyword do in C#?", 1,
                "Declares a dynamically typed variable resolved at runtime",
                "Infers the variable's static type at compile time",
                "Declares a variable that can hold any type, like object",
                "Marks a variable as mutable"),

            Q(1, "Which collection gives O(1) average lookup by key?", 2,
                "List<T>",
                "LinkedList<T>",
                "Dictionary<TKey, TValue>",
                "SortedList<TKey, TValue>"),

            Q(2, "What is the value of `3 / 2` in C#?", 0,
                "1",
                "1.5",
                "2",
                "A compile error"),

            Q(3, "What does 'async' alone do to a method?", 3,
                "Runs the method on a background thread",
                "Makes the method return immediately",
                "Creates a new Task and queues it",
                "Enables 'await' inside it and wraps the return in a Task"),

            Q(4, "Which statement about 'string' in .NET is true?", 1,
                "Strings are mutable in place",
                "Strings are immutable; every change makes a new instance",
                "Strings are value types stored on the stack",
                "Strings are limited to 2048 characters")
        }
    };

    private static QuestionDeck JavaScriptQuirks() => new()
    {
        Id = "js-quirks",
        Name = "JavaScript Quirks",
        Description = "Coercion, scoping and the famously strange bits.",
        SecondsPerQuestion = 20,
        Questions = new List<QuestionView>
        {
            Q(0, "What does `typeof null` return?", 2,
                "\"null\"",
                "\"undefined\"",
                "\"object\"",
                "\"boolean\""),

            Q(1, "What is `[] + []` in JavaScript?", 0,
                "An empty string",
                "0",
                "NaN",
                "A TypeError"),

            Q(2, "What is the difference between `==` and `===`?", 1,
                "No difference; === is just a style preference",
                "=== compares without type coercion, == coerces types first",
                "== compares references, === compares values",
                "=== only works on numbers"),

            Q(3, "What does `let` give you that `var` does not?", 0,
                "Block scoping instead of function scoping",
                "Immutability after assignment",
                "Automatic type checking",
                "Hoisting to the top of the function"),

            Q(4, "What is `0.1 + 0.2 === 0.3`?", 1,
                "true",
                "false",
                "NaN",
                "A SyntaxError")
        }
    };

    private static QuestionDeck BigO() => new()
    {
        Id = "big-o",
        Name = "Big-O Sprint",
        Description = "Complexity of the operations you reach for every day.",
        SecondsPerQuestion = 15,
        Questions = new List<QuestionView>
        {
            Q(0, "Average time complexity of binary search on a sorted array?", 1,
                "O(1)",
                "O(log n)",
                "O(n)",
                "O(n log n)"),

            Q(1, "Best average complexity for a general-purpose comparison sort?", 2,
                "O(n)",
                "O(log n)",
                "O(n log n)",
                "O(n squared)"),

            Q(2, "Appending to a dynamic array (amortised)?", 0,
                "O(1)",
                "O(log n)",
                "O(n)",
                "O(n squared)"),

            Q(3, "Looking up a value in a balanced binary search tree?", 1,
                "O(1)",
                "O(log n)",
                "O(n)",
                "O(n log n)"),

            Q(4, "Two nested loops, each over all n items?", 3,
                "O(n)",
                "O(n log n)",
                "O(log n)",
                "O(n squared)")
        }
    };
}
