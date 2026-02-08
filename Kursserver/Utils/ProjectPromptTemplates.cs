namespace Kursserver.Utils
{
    public static class ProjectPromptTemplates
    {

        public static string GenerateWebProject(string projectType, int difficulty, List<object> previousProjects = null)
        {
            var difficultyGuidelines = GetProjectDifficultyGuidelines(difficulty);
            var projectTypeGuidelines = GetProjectTypeGuidelines(projectType);
            var techStack = GetTechStack(projectType);

            var historySection = "";

            if (previousProjects != null && previousProjects.Any())
            {
                historySection = $@"
                IMPORTANT: This user has already received these project ideas. DO NOT repeat them:

                {string.Join("\n", previousProjects.Select((proj, i) =>
                    {
                        dynamic project = proj;
                        return $"{i + 1}. {project.Title}: {project.Description}";
                    }))}

                Generate something COMPLETELY DIFFERENT from the above projects.
                ";
            }
            return $@"You are a patient web development instructor creating Frontend Mentor-style projects for people learning to code.

TASK: Generate a beginner-friendly web project using {techStack}.

STUDENT LEVEL: Difficulty {difficulty}/5 (1 = just started, 5 = early intermediate)
{difficultyGuidelines}

PROJECT TYPE: {projectType}
{projectTypeGuidelines}

CRITICAL RULES:
- Students are BEGINNERS - keep it achievable and encouraging
- Focus on ONE main learning goal per project
- Provide clear, specific design requirements (colors, fonts, spacing)
- Include all necessary assets descriptions (images, icons needed)
- Give exact specifications so students know when they're ""done""
- No frameworks or libraries - vanilla {techStack} only

PROJECT STYLE (like Frontend Mentor):
- Clear visual design to replicate
- Specific requirements (colors, fonts, sizes)
- User stories (what the user should be able to do)
- Bonus challenges for students who want more

!!!!! CRITICAL FORMAT REQUIREMENT !!!!!

Your response MUST follow this EXACT template.
Copy this structure and fill in the content:

TITLE: [Short, catchy project name]

DESCRIPTION:
[2-4 sentences describing what the student will build. Explain the purpose and what they'll learn.]

DIFFICULTY: {difficulty}/5

TECH STACK: {techStack}

LEARNING GOALS:
[Bullet list of 3-5 specific skills this project teaches]

USER STORIES:
[List what users should be able to do with the finished project]
- Users should be able to...
- Users should be able to...

DESIGN SPECS:
[Exact specifications for the design]
- Colors: (provide hex codes)
- Fonts: (provide font names and sizes)
- Spacing: (provide specific pixel values)
- Layout: (describe the layout structure)

ASSETS NEEDED:
[List any images, icons, or assets the student needs]
- [Description of each asset needed]

STARTER HTML:
[Provide the basic HTML structure - DO NOT wrap in markdown code fences, just raw HTML]

SOLUTION HTML:
[Complete HTML solution with comments - DO NOT wrap in markdown code fences, just raw HTML]

SOLUTION CSS:
[Complete CSS solution with comments - DO NOT wrap in markdown code fences, just raw CSS, or N/A]

SOLUTION JS:
[Complete JavaScript solution with comments, if applicable - DO NOT wrap in markdown code fences, just raw JavaScript, or N/A]

BONUS CHALLENGES:
[2-3 optional challenges for students who finish early]

MANDATORY FORMATTING RULES:
- Each header MUST be on its own line
- Headers MUST be in ALL CAPS exactly as shown
- Headers MUST be followed by a colon (:)
- Content must come AFTER the header
- Sections must be in this EXACT order
- Do NOT skip any sections
- Do NOT rename any sections
- Do NOT add extra sections
- Images MUST have max-height of 50% (use inline style or CSS)
- All <img> tags MUST use src={{dummyPic}} (use curly braces, not quotes)
- Example: <img src={{dummyPic}} alt=""description"" />
- Do NOT use external URLs, ONLY use {{dummyPic}}
- If a section doesn't apply (e.g., JS for HTML-only), write ""N/A""

Now generate the project following this EXACT format.";
        }

        private static string GetProjectDifficultyGuidelines(int difficulty)
        {
            return difficulty switch
            {
                1 => @"Level 1: First HTML project
- Single page with basic elements only
- Use only: headings (h1-h3), paragraphs, images, links
- No complex layouts - just vertical stacking
- Focus on proper HTML structure and semantics
- Example: Simple profile card, basic article page",

                2 => @"Level 2: Learning structure
- Single page with more elements
- Can use: lists, divs, sections, basic forms
- Introduce simple CSS if applicable (colors, fonts, basic spacing)
- Simple layouts (centered content)
- Example: Recipe card, simple landing section",

                3 => @"Level 3: Building confidence
- Single page with intentional layout
- CSS: flexbox basics, simple responsive design
- More styling: borders, shadows, hover states
- Forms with multiple inputs
- Example: Pricing card, testimonial component",

                4 => @"Level 4: Getting comfortable
- Multi-section page or simple multi-page site
- CSS: flexbox layouts, CSS Grid basics, media queries
- Interactive elements with CSS (hover, focus states)
- JavaScript: basic DOM manipulation, simple events
- Example: Product preview, FAQ accordion",

                5 => @"Level 5: Early intermediate
- Complete page or small multi-page site
- Responsive design (mobile, tablet, desktop)
- JavaScript: form validation, interactive components
- More complex layouts with Grid and Flexbox
- Example: Landing page, interactive form with validation",

                _ => "Keep it simple and beginner-friendly"
            };
        }

        private static string GetProjectTypeGuidelines(string projectType)
        {
            return projectType.ToLower() switch
            {
                "html" => @"HTML ONLY PROJECT:
- Focus purely on semantic HTML structure
- No styling (browser defaults only)
- Emphasize: proper element choice, accessibility, document structure
- Students learn: when to use which HTML elements
- Output should be functional but unstyled",

                "html-css" or "html+css" => @"HTML + CSS PROJECT:
- Focus on visual design and layout
- Clean, semantic HTML with thoughtful CSS
- Emphasize: layout techniques, visual hierarchy, spacing
- Students learn: translating a design to code
- Provide exact design specs (colors, fonts, sizes)",

                "html-css-js" or "html+css+js" or "html-css-javascript" => @"HTML + CSS + JAVASCRIPT PROJECT:
- Focus on interactivity and user experience
- Build on HTML/CSS skills with JS functionality
- Emphasize: DOM manipulation, event handling, user feedback
- Students learn: making pages interactive
- Keep JS simple - vanilla only, no frameworks",

                _ => "Focus on clean, semantic code and good practices"
            };
        }

        private static string GetTechStack(string projectType)
        {
            return projectType.ToLower() switch
            {
                "html" => "HTML",
                "html-css" or "html+css" => "HTML and CSS",
                "html-css-js" or "html+css+js" or "html-css-javascript" => "HTML, CSS, and JavaScript",
                _ => projectType
            };
        }
    }
}