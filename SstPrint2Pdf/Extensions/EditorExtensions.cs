using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.EditorInput;
using SstPrint2Pdf.AcDrawing;

namespace SstPrint2Pdf.Extensions
{
    public static  class EditorExtensions
    {
        

        
        public static PromptSelectionResult GetSelection(this Editor editor, string promptObjMessage, SelectionFilter filter)
        {
            var selOpt = new PromptSelectionOptions();

            selOpt.MessageForAdding = String.Format("\n{0}", promptObjMessage);
            selOpt.AllowDuplicates = false;

            return editor.GetSelection(selOpt, filter);

        }
        public static PromptSelectionResult GetSelection(this Editor editor, string promptObjMessage,
                                        SelectionFilter filter,
                                        string promptKeyWordMessage,
                                        IEnumerable<string> keyWords,
                                        Action<string> actionKeyWord)
        {
            var selOpt = new PromptSelectionOptions();


            foreach (var keyword in keyWords)
            {
                selOpt.Keywords.Add(keyword);

            }

            var kwds = selOpt.Keywords.GetDisplayString(true);

            selOpt.MessageForAdding = String.Format("\n{0} или {1} {2}", promptObjMessage, promptKeyWordMessage, kwds);
            selOpt.AllowDuplicates = false;
            selOpt.KeywordInput += (s, e) => actionKeyWord(e.Input);

            return editor.GetSelection(selOpt, filter);

        }


        public static PromptSelectionResult GetObjectIdsInRegion(this Editor editor, string promptObjMessage, SelectionFilter filter)
        {
            PromptResult prsKw;
            PromptSelectionResult prselres;
            string msg;
            do
            {

                prselres = editor.GetSelection(String.Format("\n{0}", promptObjMessage), filter);
                if (prselres.Status == PromptStatus.OK) break;

                msg = "\nОбъекты не выбраны. Повторить?";
                prsKw = PromptForKeywordSelection(editor, msg, new[] { "Да", "Нет" }, false, "Да");


                if (prsKw.Status != PromptStatus.OK) break;
            }
            while (prsKw.StringResult != "Нет");

            return prselres;

        }
        //not tested
        public static PromptResult PromptStringWithKeywords(this Editor editor, string promptStringMessage,
                                                     string promptKeyWordMessage,
                                                      IEnumerable<string> keywords,

                                                       string defaultKeyword = "")
        {
            var promptStringOptions = new PromptStringOptions("\n" + promptStringMessage + " или " + promptKeyWordMessage + ": ");

            foreach (var keyword in keywords)
            {
                promptStringOptions.Keywords.Add(keyword);

            }
            if (defaultKeyword != "")
            {
                promptStringOptions.Keywords.Default = defaultKeyword;
            }



            promptStringOptions.AppendKeywordsToMessage = true;

            var keywordResult = editor.GetString(promptStringOptions);
            return keywordResult;
        }

        public  static PromptResult PromptForKeywordSelection(this Editor editor, string promptMessage,
                                                       IEnumerable<string> keywords,
                                                        bool allowNone,
                                                        string defaultKeyword = "")
        {
            var promptKeywordOptions = new PromptKeywordOptions("\n" + promptMessage) { AllowNone = allowNone };
            foreach (var keyword in keywords)
            {
                promptKeywordOptions.Keywords.Add(keyword);
            }
            if (defaultKeyword != "")
            {
                promptKeywordOptions.Keywords.Default = defaultKeyword;
            }
            promptKeywordOptions.AppendKeywordsToMessage = true;
            var keywordResult = editor.GetKeywords(promptKeywordOptions);
            return keywordResult;
        }


       public static PromptResult Getstringvalue(this Editor editor, string message)
        {

            var pso = new PromptStringOptions(String.Format("\n{0}", message));
            pso.DefaultValue = String.Empty;
            pso.AllowSpaces = true;

            return editor.GetString(pso);


        }

        public static string GetStringValue(this Editor editor, string promptMessage)
        {

            PromptResult prsKw;
            string msg;
            do
            {
                var prs = Getstringvalue(editor, promptMessage);
                if (prs.Status == PromptStatus.OK && !string.IsNullOrEmpty(prs.StringResult)) return prs.StringResult;

                msg = "\nНекорректный ввод. Необходимо ввести один или несколько символов. Повторить?";
                prsKw = PromptForKeywordSelection(editor, msg, new[] { "Да", "Нет" }, false, "Да");


                if (prsKw.Status != PromptStatus.OK) break;



            } while (prsKw.StringResult != "Нет");


            return null;


        }

        public static int GetNaturalValue(this Editor editor, string promptMessage,  int defaultval, bool allownone)
        {

            PromptResult prsKw;
            
            string msg;
            var printopt = new PromptIntegerOptions(promptMessage);
            printopt.AllowNegative = false;
            printopt.AllowZero = false;
            printopt.DefaultValue = defaultval;
            printopt.AllowNone = allownone;
            do
            {
                var prs = editor.GetInteger(printopt);
                if (prs.Status == PromptStatus.OK ) return prs.Value;

                msg = "\nНекорректный ввод. Необходимо ввести натуральное число. Повторить?";
                prsKw = PromptForKeywordSelection(editor, msg, new[] { "Да", "Нет" }, false, "Да");


                if (prsKw.Status != PromptStatus.OK) break;



            } while (prsKw.StringResult != "Нет");


            return -1;


        }

       
    
    
    }
}
