namespace Kursserver.Utils
{
    public static class ExercisePromptTemplates
    {
        public static string GeneratePracticeAsserts(
            string topic,
            string language,
            int difficulty,
            List<object>? previousExercises = null) // 1-5
        {
            var topicGuidelines = GetTopicGuidelines(topic);
            var difficultyGuidelines = GetDifficultyGuidelines(difficulty);
            var testingFramework = GetTestingFramework(language);
            var languageSpecificNotes = GetLanguageSpecificNotes(language, topic);

            var historySection = "";

            if (previousExercises != null && previousExercises.Any())
            {
                historySection = $@"
IMPORTANT: This user has already completed these exercises. DO NOT repeat them:

{string.Join("\n", previousExercises.Select((ex, i) =>
                {
                    dynamic exercise = ex;
                    return $"{i + 1}. {exercise.Title}: {exercise.Description}";
                }))}

Your new exercise MUST be different in ALL of these ways:
1. Different problem domain (if they did shopping, use gaming; if gaming, use science)
2. Different operations (if they filtered, use mapping; if mapped, use reducing)
3. Different context/theme (sports, cooking, travel, weather, etc.)
4. Different edge cases to handle (if edge cases are appropiate for the exercise)

DO NOT create variations of the above exercises.
DO NOT reuse similar scenarios, even with different names.
";
            }

            return $@"You are a patient programming instructor creating practice exercises for people who JUST started learning to code.

TASK: Generate test assertions to help students practice {topic} in {language}.

STUDENT LEVEL: Difficulty {difficulty}/5 (1 = just started, 5 = early intermediate)
{difficultyGuidelines}

TOPIC: {topic}
{topicGuidelines}

TESTING FRAMEWORK: {testingFramework}
{languageSpecificNotes}

CRITICAL RULES:
- Students are BEGINNERS - keep it simple and encouraging
- Level 1-2: Only use concepts they definitely know (variables, basic operators)
- Level 3-4: Can introduce slightly more concepts but nothing complex
- Level 5: Early intermediate - still no advanced patterns or complex logic
- Each assert should test ONE concept clearly
- Use realistic but simple variable names (age, name, numbers, isValid)
- Include a brief comment explaining what each assert tests

EXERCISE STYLE (like Codewars):
- Clear task description (what should the function do?)
- Show example input => expected output
- List assumptions (what can students assume about inputs?)
- Provide function signature (starter code)
- Generate 5-7 test assertions



!!!!! CRITICAL FORMAT REQUIREMENT !!!!!

Your response MUST follow this EXACT template.
Copy this structure and fill in the content:

TITLE: [Short, clear title for the exercise]

DESCRIPTION:
[2-4 sentences describing the task in plain beginner-friendly language. Explain WHAT the student needs to build, not how to build it.]

EXAMPLE:
[Show 2-3 examples of input => expected output]

ASSUMPTIONS:
[Bullet list of what students can assume about inputs, edge cases, etc.]

FUNCTION SIGNATURE:
[Provide the starter function code that students will complete]

SOLUTION:
[Complete, working solution code with comments explaining the approach]

ASSERTS:
[5-7 test assertions with comments above each one explaining what it tests]

MANDATORY FORMATTING RULES:
- Each header MUST be on its own line
- Headers MUST be in ALL CAPS exactly as shown: TITLE, DESCRIPTION, EXAMPLE, ASSUMPTIONS, FUNCTION SIGNATURE, SOLUTION, ASSERTS
- Headers MUST be followed by a colon (:)
- Content must come AFTER the header
- Sections must be in this EXACT order
- Do NOT skip any sections
- Do NOT rename any sections
- Do NOT add extra sections

CORRECT FORMAT EXAMPLE:

TITLE: Array Sum Calculator

DESCRIPTION:
Write a function that takes an array of numbers and returns their sum.
This exercise helps you practice loops and working with arrays.
If the array is empty, return 0.

EXAMPLE:
[1, 2, 3] => 6
[10, -5, 3] => 8
[] => 0

ASSUMPTIONS:
Input is always an array of numbers
Array can be empty
Numbers can be negative
No need to validate input

FUNCTION SIGNATURE:
function sum(numbers) {{
  return 0;
}}

SOLUTION:
function sum(numbers) {{
  // Handle empty array
  if (numbers.length === 0) return 0;
  
  // Add up all numbers
  let total = 0;
  for (let num of numbers) {{
    total += num;
  }}
  return total;
}}

ASSERTS:
// Sum of positive numbers
expect(sum([1, 2, 3])).toBe(6);
// Handle negative numbers
expect(sum([10, -5, 3])).toBe(8);
// Empty array returns zero
expect(sum([])).toBe(0);
// Single number
expect(sum([42])).toBe(42);
// All zeros
expect(sum([0, 0, 0])).toBe(0);


Do NOT include:
- Full test methods, classes, or setup code
- Import/using statements
- Long explanations after the asserts

One assert per line with a comment above it.

NOW: Generate assertions for {topic} in {language} at level {difficulty}/5.";
        }

        private static string GetTopicGuidelines(string topic)
        {
            return topic.ToLower() switch
            {
                "variables and datatypes" or "variables" or "datatypes" =>
    @"TOPIC FOCUS: Variables and Data Types
Test concepts like:
- Declaring and assigning variables
- Different data types (int, string, bool, float/double)
- Type checking
- Default values
- Simple type conversions",

                "numbers" =>
    @"TOPIC FOCUS: Numbers and Basic Arithmetic
Test concepts like:
- Working with numbers (integers, decimals)
- Arithmetic operators (+, -, *, /, %)
- Assignment operators (=, +=, -=)
- Number comparisons
- Simple calculations",

                "conditionals" or "if" or "else" or "switch" =>
    @"TOPIC FOCUS: Conditionals (if, else, switch)
Test concepts like:
- Simple if statements
- if-else conditions
- Basic switch/case statements
- Truthy and falsy values
- Comparison operators (==, !=, <, >, <=, >=)
- Logical operators (&&, ||, !)
- Simple condition results",

                "loops" or "for" or "while" or "do-while" =>
    @"TOPIC FOCUS: Loops (for, while, do-while)
Test concepts like:
- Loop counter values
- Loop termination conditions
- Accumulated values after loop runs
- Number of iterations
- Simple loop outputs",

                "functions" or "methods" =>
    @"TOPIC FOCUS: Functions
Test concepts like:
- Function return values
- Passing parameters
- Simple calculations in functions
- Function output with different inputs
- Basic function behavior",

                "arrays" or "lists" =>
    @"TOPIC FOCUS: Arrays
Test concepts like:
- Array length/size
- Accessing elements by index
- First and last elements
- Checking if array contains a value
- Simple array operations",

                "objects" or "classes" =>
    @"TOPIC FOCUS: Objects
Test concepts like:
- Accessing object properties
- Setting property values
- Checking if property exists
- Simple object comparisons
- Object property types",

                "strings" or "string methods" =>
    @"TOPIC FOCUS: Strings and String Methods
Test concepts like:
- String length
- Uppercase and lowercase
- Finding substrings
- String concatenation
- Accessing characters by index",

                "dom" or "dom basics" =>
    @"TOPIC FOCUS: DOM Basics (JavaScript only)
Test concepts like:
- Getting elements by ID
- Element text content
- Element existence
- Simple attribute values
- Basic DOM queries",

                "events" =>
    @"TOPIC FOCUS: Events (JavaScript only)
Test concepts like:
- Event handler existence
- Event type checking
- Simple event properties
- Click event basics
- Event listener attachment",

                _ => $@"TOPIC FOCUS: {topic}
Focus on fundamental concepts appropriate for beginners."
            };
        }

        private static string GetDifficultyGuidelines(int level)
        {
            return level switch
            {
                1 =>
    @"LEVEL 1 - ABSOLUTE BEGINNER:
- Student just learned what variables are
- Only test the most basic concepts
- Use very simple values (small numbers, short strings)
- Test ONE thing per assertion
- No tricky edge cases
- Example: Check if a number equals 5, check if a string equals ""hello""
- Be encouraging - these are their first steps!",

                2 =>
    @"LEVEL 2 - GETTING STARTED:
- Student understands basic variables and simple operations
- Can combine 2 simple concepts
- Still use straightforward values
- Introduce basic comparisons (greater than, less than)
- No complex expressions
- Example: Check if age > 18, check string length",

                3 =>
    @"LEVEL 3 - BUILDING CONFIDENCE:
- Student is comfortable with basics
- Can handle slightly more complex assertions
- Introduce collections (arrays) at basic level
- Can test simple function outputs
- Still avoid complex logic
- Example: Check array length, test function return value",

                4 =>
    @"LEVEL 4 - PROGRESSING WELL:
- Student can combine multiple concepts
- Comfortable with arrays, functions, and objects
- Can handle assertions with 2-3 steps of logic
- Introduce edge cases gently (empty string, zero)
- Example: Test array contains value, check object property",

                5 =>
    @"LEVEL 5 - EARLY INTERMEDIATE:
- Student has solid foundation but NOT advanced yet
- Can handle more realistic scenarios
- Test combinations of concepts
- Include some edge cases
- Still NO complex algorithms or patterns
- Example: Test function with edge case input, verify loop results
- Remember: They still can't build complex apps!",

                _ => "Focus on beginner-appropriate concepts."
            };
        }

        private static string GetTestingFramework(string language)
        {
            return language.ToLower() switch
            {
                "c#" or "csharp" => "NUnit: Assert.AreEqual(expected, actual), Assert.IsTrue(condition), Assert.IsFalse(condition), Assert.IsNull(value), Assert.IsNotNull(value)",
                "javascript" or "js" => "Jest: expect(actual).toBe(expected), expect(actual).toEqual(expected), expect(condition).toBeTruthy(), expect(array).toContain(value)",
                "python" => "pytest: assert actual == expected, assert condition, assert value in collection",
                "java" => "JUnit: assertEquals(expected, actual), assertTrue(condition), assertFalse(condition), assertNotNull(value)",
                "typescript" or "ts" => "Jest: expect(actual).toBe(expected), expect(actual).toEqual(expected), expect(condition).toBeTruthy()",
                _ => $"Standard testing framework for {language}"
            };
        }

        private static string GetLanguageSpecificNotes(string language, string topic)
        {
            var lang = language.ToLower();
            var topicLower = topic.ToLower();

            // DOM and Events are JavaScript only
            if (topicLower is "dom" or "dom basics" or "events")
            {
                if (lang is not "javascript" and not "js")
                {
                    return $"\nNOTE: DOM and Events are JavaScript concepts. Generate equivalent {language} concepts like file I/O or console interaction instead.";
                }
            }

            return lang switch
            {
                "javascript" or "js" => "\nJavaScript notes: Use let/const for variables. Remember === for strict equality.",
                "python" => "\nPython notes: Use snake_case for variables. No semicolons needed.",
                "c#" or "csharp" => "\nC# notes: Use PascalCase for methods, camelCase for variables.",
                "java" => "\nJava notes: Use camelCase for variables and methods.",
                _ => ""
            };
        }
    }
}
