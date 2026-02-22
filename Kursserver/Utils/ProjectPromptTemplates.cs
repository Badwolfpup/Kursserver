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

                Your new project MUST be different in ALL of these ways:
                1. Different theme/domain (if they built a restaurant site, use a completely unrelated domain)
                2. Different layout structure (if they built a card layout, use a dashboard or gallery or timeline instead)
                3. Different visual style (if previous was minimal, make this bold and colorful, or vice versa)
                4. Different primary learning focus (if they practiced flexbox, focus on grid or forms or animations)

                DO NOT create variations of the above projects.
                DO NOT reuse similar layouts or themes, even with different names.
                DO NOT use the same project structure — if they keep getting ""card components"", switch to a completely different format.
                ";
            }
            return $@"You are a creative web development instructor who designs engaging, original projects inspired by Frontend Mentor and devchallenges.io. You never repeat yourself and always surprise students with fresh, unexpected project ideas.

TASK: Generate a unique and creative web project using {techStack}.

STUDENT LEVEL: Difficulty {difficulty}/5 (1 = Newbie, 2 = Junior, 3 = Intermediate, 4 = Advanced, 5 = Guru)
{difficultyGuidelines}

PROJECT TYPE: {projectType}
{projectTypeGuidelines}

CREATIVITY & VARIETY:
- Be original! Avoid generic projects like ""portfolio page"" or ""todo app""
- Pick an unexpected, fun theme from domains like: space agency dashboard, vinyl record store, pet adoption center, food festival guide, cozy bookshop, surf forecast tracker, botanical garden catalog, escape room booking, vintage camera shop, mountain hiking guide, retro arcade scoreboard, coffee roastery showcase, underwater dive log, astronomy event calendar, indie game studio page
- Vary the project STRUCTURE — don't always make ""a card with an image and text"". Mix in: multi-column layouts, dashboards, interactive forms, data displays, comparison views, timelines, galleries, pricing tables
- Each project should feel like a real-world product, not a homework assignment
- Draw inspiration from both Frontend Mentor challenges and devchallenges.io projects

CRITICAL RULES:
- Match complexity strictly to the difficulty level specified above
- Level 1-2: Keep it achievable and encouraging, focus on fundamentals
- Level 3: Real layouts and styling, expect comfort with CSS positioning
- Level 4-5: Real complexity is expected — multi-section sites, interactivity, polished designs
- The gap between each level should be clearly noticeable
- Focus on ONE main learning goal per project
- Provide clear, specific design requirements (colors, fonts, spacing)
- Include all necessary assets descriptions (images, icons needed)
- Give exact specifications so students know when they're ""done""
- No frameworks or libraries - vanilla {techStack} only

PROJECT STYLE (like Frontend Mentor / devchallenges.io):
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
- For images, choose from these available options: coral, mountain, city, forest, beach
- Use this EXACT format for img tags: <img src=""{{image:NAME}}"" alt=""description"" /> where NAME is one of the available options
- Example: <img src=""{{image:coral}}"" alt=""coral reef"" /> or <img src=""{{image:forest}}"" alt=""forest path"" />
- Pick the image that best fits the project theme
- Do NOT use external URLs or relative paths, ONLY use the {{image:NAME}} format
- If a section doesn't apply (e.g., JS for HTML-only), write ""N/A""

Now generate the project following this EXACT format.";
        }

        private static string GetProjectDifficultyGuidelines(int difficulty)
        {
            return difficulty switch
            {
                1 => @"Level 1 - NEWBIE (Frontend Mentor: Newbie):
- Single page with basic elements only
- Use only: headings (h1-h3), paragraphs, images, links
- No complex layouts - just vertical stacking
- Focus on proper HTML structure and semantics
- Think Frontend Mentor Newbie: QR code component, single product card, order summary
- Example: Simple profile card, basic article page",

                2 => @"Level 2 - JUNIOR (Frontend Mentor: Junior):
- Single page with more elements and intentional styling
- Can use: lists, divs, sections, basic forms
- CSS: colors, fonts, basic spacing, centered layouts
- Simple but polished visual presentation
- Think Frontend Mentor Junior: Interactive rating component, NFT preview card, tip calculator
- Example: Recipe card, simple landing section, stats preview",

                3 => @"Level 3 - INTERMEDIATE (Frontend Mentor: Intermediate):
- Single page with intentional layout and real structure
- CSS: flexbox basics, simple responsive design, media queries
- More styling: borders, shadows, hover states, transitions
- Forms with multiple inputs and basic validation
- Think Frontend Mentor Intermediate: Pricing toggle, testimonials grid, newsletter sign-up with validation
- Think devchallenges.io: Windbnb listing page, Interior Consultant
- Example: Pricing card, testimonial component, interactive form",

                4 => @"Level 4 - ADVANCED (Frontend Mentor: Advanced):
- Multi-section page or small multi-page site
- CSS: complex flexbox + CSS Grid layouts, full responsive design (mobile/tablet/desktop)
- Interactive elements: animations, transitions, toggle states
- JavaScript: DOM manipulation, event handling, dynamic content
- Think Frontend Mentor Advanced: E-commerce product page, job listings with filter, multi-step form
- Think devchallenges.io: Checkout page, My Gallery, Weather app
- Example: Product preview with image gallery, FAQ accordion, filterable card grid",

                5 => @"Level 5 - GURU (Frontend Mentor: Guru):
- Complete, polished multi-section site or complex single-page app
- Full responsive design with smooth transitions and animations
- Complexity scales with tech stack:
  * HTML only: Complex semantic structures, accessibility (ARIA), SEO best practices, structured data
  * HTML+CSS: Multi-section responsive sites, CSS animations, advanced Grid layouts, dark/light themes
  * HTML+CSS+JS: API consumption (fetch), localStorage, complex interactivity (dashboards, multi-step forms, drag-and-drop)
- Think Frontend Mentor Guru: REST Countries API, Rock Paper Scissors, Kanban task management
- Think devchallenges.io: GitHub Jobs, Country Quiz, Chat application
- Example: Dashboard with charts, multi-step checkout, interactive data table",

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