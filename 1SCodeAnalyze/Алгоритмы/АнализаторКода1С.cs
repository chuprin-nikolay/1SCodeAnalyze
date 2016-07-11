﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using _1SCodeAnalyze.Структуры;
//туду
//.есть какая нибудь утилита анализирующая конфигу и говорящая какие функции не используются? (во всей конфе включая вызов из элементов формы)
//нужно проверить что Имена процедур и функций начинаются с заглавной буквы и если длина имени большая то должно быть несколько заглавных
//
namespace _1SCodeAnalyze
{
    class АнализаторКода1С
    {

        Dictionary<String, Модуль> Модули;
        List<FileInfo> files;
		StreamWriter sw_fileReport;
		int всегоСтрок = 0;
		int строкКомментарии;
        ConsoleSpiner spin;


        public АнализаторКода1С(List<FileInfo> files)
        {
            this.files = files;
            spin = new ConsoleSpiner();
            Модули = new Dictionary<string, Модуль>();
			string fileNameReport = Directory.GetCurrentDirectory() + "\\report_" + DateTime.Now.ToString().Replace(".", "").Replace(":", "").Replace(" ", "") + ".txt";
			FileInfo file = new FileInfo(fileNameReport);
			
			sw_fileReport = File.CreateText(fileNameReport);
			ОбойтиВсеФайлы();
			sw_fileReport.Close();
            

        }

		public void AddLog(string message)
		{
			Console.WriteLine(message);
			sw_fileReport.WriteLine(message);
		}


        private void ОбойтиВсеФайлы()
        {
            ПолучитьВсеМодулиИзФайлов();
//            Console.WriteLine("Будет проанализировано "+Модули.Count.ToString()+" текстов Модулей");
			AddLog("Всего строк в проекте " + всегоСтрок.ToString() + "\n");
			if (!КоманднаяСтрока.isAnalyzeCode)
				return;
			ПроанализироватьВсеМодули();
        }

        private void ПолучитьВсеМодулиИзФайлов()
        {
            foreach (FileInfo Файл in files)
            {


                String ИмяМодуля = Файл.Name.Replace(".Модуль.txt", "").Replace(".txt", "");
                Модуль МодульОбъекта = new Модуль(Файл);
				if(МодульОбъекта.ЕстьОшибки)System.Console.Write("x"); else System.Console.Write(".");
                всегоСтрок += МодульОбъекта.ПолучитьНомерСтрокиПоИндексу(-1);
				if (!КоманднаяСтрока.isAnalyzeCode)
					continue;
                if (!Модули.ContainsKey(ИмяМодуля))
                {
                    Модули.Add(ИмяМодуля, МодульОбъекта);
                }
            }
        }


        private void ПроанализироватьВсеМодули()
        {
            //foreach (KeyValuePair<String, Модуль> Объект in Модули)НайтиВсеФункцииИПроцедуры(Объект.Value);
            int КоличествоМодулей = Модули.Count;

           // foreach (KeyValuePair<String, Модуль> Объект in Модули)       {            }            
            foreach (KeyValuePair<String, Модуль> Объект in Модули)
            {
                
                АнализироватьЦиклы(Объект.Value);
                if (Объект.Value.ЕстьОшибки)
                {
					AddLog(((int)(100 - ((float)КоличествоМодулей / (float)Модули.Count) * 100.0f)).ToString()+"%  " + Объект.Key);
                    foreach (var T in Объект.Value.ТаблицаАнализа)
                    {
						AddLog(Объект.Key+".строка " + Объект.Value.ПолучитьНомерСтрокиПоИндексу(T.Смещение) + ": \n" + T.ОписаниеПроблемы + "\n");
                    }
                }
                КоличествоМодулей--;
            }
        }



		#region Методы поиска процедур и функций 

        /// <summary>
        /// Функция производит поиск запросов в вызываемых методах текста
        /// </summary>
        /// <param name="Текст">Текст процедуры</param>
        /// <param name="МодульОбъекта">Весь файл</param>
        /// <param name="Index">точка входа в процедуру </param>
        /// <param name="СвойствоМетода">Свойства вызываемых процедур</param>
        /// <param name="ВызывающийМетод">защита от рекурсий</param>
        /// <returns>Истина/Ложь</returns>
        private Boolean РекурсивныйПоискЗапроса(String Текст, Модуль МодульОбъекта, int Index,  СвойстваМетодов СвойствоМетода, String ВызывающийМетод, int Глубина)
        {
            int i = Console.CursorTop;
            Console.Write("                                                                                                                                                 ");
            Console.SetCursorPosition(1, i);
            Console.Write("    ");
            Console.Write(ВызывающийМетод);
            Console.SetCursorPosition( 1, i);
            spin.Turn();
            String СтекСтрокой = СвойствоМетода.ПолучитьСтекСтрокой();
            if (СтекСтрокой.Length > 100) СтекСтрокой = СтекСтрокой.Substring(0, 100) + "... ";

            if (Глубина > 20) {
                String КусокКода = "Запутаная рекурсия в  " + МодульОбъекта.file.Name + " Методе " + ВызывающийМетод + " -> " + СтекСтрокой + "\nМетод вызывает цепочку методов которые снова вызывают этот же метод, возможно переполнение стека";

                ИнформацияАнализа Анализ = new ИнформацияАнализа(Index, КусокКода, СтекСтрокой);
                МодульОбъекта.ДобавитьПроблему(Анализ);
                СвойствоМетода = new СвойстваМетодов();
                Глубина = 1;
                return false;
            }
            ТелоКода Тело = new ТелоКода(Текст, Index);

            if (Тело.КоличествоСтрок > 300&&Глубина > 0) {
                String КусокКода = "Слишком много строк кода в одном методе " + ВызывающийМетод + " -> " + СтекСтрокой + "\n, для повышения массштабируемости и командной поддержки кода, желательно разбить его на несколько методов";
                МодульОбъекта.ДобавитьПроблему(new ИнформацияАнализа(Index, КусокКода, СтекСтрокой)); 
            }

			if (Тело.ЕстьЗапрос()) {
                СвойствоМетода.ЕстьЗапрос = true;
                if (Глубина == 0)
                {
                    МодульОбъекта.ДобавитьПроблему(Тело.ПолучитьАнализ());
                }
                else
                {
                    return true;
                }
            }
			if (КоманднаяСтрока.isSearchChainCalls) {
				var цепочныйВызов = Тело.ВызовыПоЦепочке ();
				if (цепочныйВызов != null)
					МодульОбъекта.ДобавитьПроблему (цепочныйВызов);
			}
            //Ищем другие вызываемые процедуры
            MatchCollection Найдены = Тело.НайтиВызовы();
            foreach (Match Вызов in Найдены)
            {
				if(ВызывающийМетод.ToUpper().Contains(Вызов.Groups[1].Value.ToUpper()))continue;//self call //рекурсия
				var ПоискМетода = new Regex(@"(Процедур|Функци|procedur|functio)[аяne][\s]*?" + Regex.Escape(Вызов.Groups[1].Value) + @"[\s]*?\(([\S\s]*?)(Конец|end)\1[ыиen]", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                Match Найден = ПоискМетода.Match(МодульОбъекта.Текст);
                if (!Найден.Success)
                    continue;
                //натравим на эти процедуры эту же функцию
                СвойствоМетода.ДобавитьВызов(Вызов.Groups[1].Value);
                if (РекурсивныйПоискЗапроса(Найден.Groups[2].Value, МодульОбъекта, Найден.Index, СвойствоМетода, Вызов.Groups[1].Value, Глубина + 1))
                {
                    СвойствоМетода.ЕстьЗапрос = true;
                    
                    if (Глубина == 0)
                    {
                        СтекСтрокой = СвойствоМетода.ПолучитьСтекСтрокой() + "Запрос()";
                        String КусокКода = (Текст.Length > 20 ? Текст.Substring(0, 20).Trim() : "") + "\n...\n" + СтекСтрокой;
                        ИнформацияАнализа Анализ = new ИнформацияАнализа(Вызов.Groups[1].Index + Index, КусокКода, СтекСтрокой);
                        МодульОбъекта.ДобавитьПроблему(Анализ);
                        СвойствоМетода = new СвойстваМетодов().ДобавитьВызов(ВызывающийМетод);
                        continue;
                    }
                    return true;
                }
                else {
                    СвойствоМетода.УдалитьВызов(Вызов.Groups[1].Value);
                }
            }
            return false;
        }

		/// <summary>
		/// Метод производит поиск всех всех функции И процедур.
		/// отмечая их свойства: с запросом и стек вызовов
		/// </summary>
		/// <param name="МодульОбъекта">Модуль объекта.</param>
		private void НайтиВсеФункцииИПроцедуры(Модуль МодульОбъекта)
        {
            var ПоискФункций = new Regex(@"^(?!\/\/)[^\.\/]*?(procedur|functio|Процедур|Функци)[enая][\s]*?([А-Яа-яa-z0-9_]*?)[\s]?\(([\S\s]*?)(Конец|End)\1[enыи]", RegexOptions.IgnoreCase | RegexOptions.Multiline);
			MatchCollection Найдены = ПоискФункций.Matches(МодульОбъекта.Текст);
            foreach (Match Функция in Найдены)
            {
				СвойстваМетодов СвойствоМетода = new СвойстваМетодов();
				СвойствоМетода.ЕстьЗапрос = РекурсивныйПоискЗапроса(Функция.Groups[3].Value, МодульОбъекта, Функция.Index, СвойствоМетода, Функция.Groups[2].Value , 1);
				СвойствоМетода.Index = Функция.Index;
				//МодульОбъекта.ДобавитьМетод(Функция.Groups[2].Value, СвойствоМетода);
            }
        }
		#endregion

		public void АнализироватьЦиклы(Модуль МодульОбъекта)
        {

            var ПоискФункций = new Regex(@"(Для|Пока|for|while).+(Цикл|do)[\S\s]*?(КонецЦикла|endfor|enddo)", RegexOptions.IgnoreCase | RegexOptions.Multiline); //(Для|Пока).+ нужен иначе выражение находит ....Цикла;   код код код для цикл   КонецЦикла;
            //  необходимо переработать это выражение т.к если попадаются вложенные циклы то обрабатываются неверно
            MatchCollection Найдены = ПоискФункций.Matches(МодульОбъекта.Текст);
            foreach (Match Функция in Найдены)           
                РекурсивныйПоискЗапроса(Функция.Value, МодульОбъекта, Функция.Index, new СвойстваМетодов(), "", 0);
        }
    }


 //   static void Main(string[] args)
  //  {
   //     ConsoleSpiner spin = new ConsoleSpiner();
    //    Console.Write("Working....");
     //   while (true)
      //  {
      //      spin.Turn();
       // }
   // }

    public class ConsoleSpiner
    {
        int counter;
        public ConsoleSpiner()
        {
            counter = 0;
        }
        public void Turn()
        {
            counter++;
            switch (counter % 4)
            {
                case 0: Console.Write("/"); break;
                case 1: Console.Write("-"); break;
                case 2: Console.Write("\\"); break;
                case 3: Console.Write("|"); break;
            }
            Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
        }
    }

}

