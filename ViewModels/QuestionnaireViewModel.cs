using Avalonia.Threading;
using Hl7.Fhir.Model;
using MakeOrderR4v2.Models;
using NLog;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MakeOrderR4v2.ViewModels
{
    public class QuestionnaireViewModel: ViewModelBase
    {
        #region Fields and Properties
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        public override string Name { get => "Опросник"; }
        public IObservable<bool> canExecuteGetQuestions { get; set; }
        public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> GetQuestionsCommand { get; set; }
        public IObservable<bool> canExecuteCreateBundleWithAnswers { get; set; }
        public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> CreateBundleWithAnswersCommand { get; set; }
        public Order Order { get; private set; }
        private bool isWaiting;
        public bool IsWaiting
        {
            get => isWaiting;
            set => this.RaiseAndSetIfChanged(ref isWaiting, value);
        }
        private List<QuestionnaireResponseAnswerElement> choiceItems;
        public List<QuestionnaireResponseAnswerElement> ChoiceItems
        {
            get => choiceItems;
            set => this.RaiseAndSetIfChanged(ref choiceItems, value);
        }

        private List<QuestionnaireResponseAnswerElement> stringItems;
        public List<QuestionnaireResponseAnswerElement> StringItems
        {
            get => stringItems;
            set => this.RaiseAndSetIfChanged(ref stringItems, value);
        }

        #endregion

        #region .ctor
        public QuestionnaireViewModel(Order order)
        {
            Order = order;
            canExecuteGetQuestions = this.WhenAnyValue(x => x.Order.BundleForQuestionnaire, (bfq) => bfq != null && bfq.Length > 0);
            GetQuestionsCommand = ReactiveCommand.CreateFromTask(async () => await System.Threading.Tasks.Task.Factory.StartNew(() => GetQuestions()), canExecuteGetQuestions);
            canExecuteCreateBundleWithAnswers = this.WhenAnyValue(x => x.ChoiceItems, x => x.StringItems, (ci, si) => ci != null && si != null);
            CreateBundleWithAnswersCommand = ReactiveCommand.CreateFromTask(async () => await System.Threading.Tasks.Task.Factory.StartNew(() => CreateBundleWithAnswers()), canExecuteCreateBundleWithAnswers);
        }

        #endregion

        #region Methods
        private void GetQuestions()
        {
            Dispatcher.UIThread.InvokeAsync(() => IsWaiting = true);
            try
            {
                logger.Info("Получаем вопросы для опросника и делим их на вопросы с выбором и вопросы с заполнением.");
                var items = Order.GetQuestionnaireResponseItems();
                Dispatcher.UIThread.InvokeAsync(() => ChoiceItems = items.Where(x => x.ItemType == Questionnaire.QuestionnaireItemType.Choice).ToList());
                Dispatcher.UIThread.InvokeAsync(() => logger.Info($"Вопросы с выбором отобраны, кол-во {ChoiceItems?.Count}."));
                Dispatcher.UIThread.InvokeAsync(() => StringItems = items.Except(ChoiceItems).ToList());
                Dispatcher.UIThread.InvokeAsync(() => logger.Info($"Вопросы с заполнением отобраны, кол-во {StringItems?.Count}."));
            }
            catch (Exception ex)
            {
                logger.Info($"Не удалось разобрать вопросы на вопросы с выбором и вопросы с заполнением по причине:\r\n{ex}.");
                Dispatcher.UIThread.InvokeAsync(() => ChoiceItems.Clear());
                Dispatcher.UIThread.InvokeAsync(() => StringItems.Clear());
                Dispatcher.UIThread.InvokeAsync(() => Order.QuestionnaireBundleWithAnswers = ex.ToString());                
            }
            finally
            {
                Dispatcher.UIThread.InvokeAsync(() => IsWaiting = false);
            }
        }

        public void CreateBundleWithAnswers()
        {
            Dispatcher.UIThread.InvokeAsync(() => IsWaiting = true);
            try
            {
                logger.Info($"Создаем бандл с ответами на вопросы.");
                List<QuestionnaireResponseAnswerElement> answerItems = ChoiceItems != null ? new List<QuestionnaireResponseAnswerElement>(ChoiceItems) : new List<QuestionnaireResponseAnswerElement>();
                if (StringItems != null)
                {
                    answerItems.AddRange(StringItems);
                }
                if (Validate.AreAllAnswersValid(answerItems))
                {
                    Dispatcher.UIThread.InvokeAsync(() => Order.CreateBundleWithAnswers(answerItems));
                    Dispatcher.UIThread.InvokeAsync(() => logger.Info($"Создан бандл с ответами на вопросы"));
                    Dispatcher.UIThread.InvokeAsync(() => logger.Trace($"\r\n{Order.QuestionnaireBundleWithAnswers}"));
                }
                else
                {
                    Dispatcher.UIThread.InvokeAsync(() => Order.QuestionnaireBundleWithAnswers = "Не все обязательные поля заполнены!!!");
                    logger.Error("Не все обязательные поля заполнены!!!");
                }
            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.InvokeAsync(() => Order.QuestionnaireBundleWithAnswers = ex.ToString());
                logger.Error($"Не удалось создать бандл с ответами на вопросы по причине:\r\n{ex}.");
            }
            finally
            {
                Dispatcher.UIThread.InvokeAsync(() => IsWaiting = false);
            }
        }
        #endregion

    }
}
