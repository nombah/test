using System.Runtime.InteropServices.ComTypes;
using Microsoft.ML;
using Microsoft.ML.Runtime.Data;
using Microsoft.ML.StaticPipe;
using Microsoft.ML.Trainers;
using WebApplication1.Models;

namespace WebApplication1.ML
{
    public class Analyze
    {
        public TeamPrediction Analyse()
        {
            var env = new LocalEnvironment();
            var reader = TextLoader.CreateReader(env, ctx => (
                    Subject: ctx.LoadText(0),
                    Body: ctx.LoadText(1),
                    Team: ctx.LoadText(2)),
                hasHeader: true, separator: ',');
            var data = reader.Read(new MultiFileSource("Bok1.csv"));
            var regression = new RegressionContext(env);
            var pipeline = reader.MakeNewEstimator()
                .Append(r => (r.Team,
                    Prediction: regression));
            var model = pipeline.Fit(data).AsDynamic;
            var prediction = model.MakePredictionFunction<MailObject, TeamPrediction>(env);
            var predictio = prediction.Predict(new MailObject {Body = "Incident Claims"});
            //    var pipeline = new LearningPipeline();
            //    pipeline.Add(new TextLoader("bok1.csv").CreateFrom<MailObject>(useHeader: true, separator: ','));
            //    pipeline.Add(new ColumnConcatenator("Features","Subject"));
            //    pipeline.Add(new GeneralizedAdditiveModelRegressor());

            //    var model = pipeline.Train<MailObject, TeamPrediction>();
            //    var evaluator = new RegressionEvaluator();
            //    var metrics = evaluator.Evaluate(model)
            //    return null;
            //}
            return predictio;
        }
    }
}