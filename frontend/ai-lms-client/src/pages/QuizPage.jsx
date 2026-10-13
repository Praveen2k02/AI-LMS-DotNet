import { useEffect, useMemo, useRef, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { ArrowLeft, ArrowRight, BrainCircuit, Check, CheckCircle2, Clock3, Sparkles, X } from "lucide-react";
import { api, errorMessage } from "../api/client";
import { useToast } from "../context/ToastContext";

export default function QuizPage() {
  const { id } = useParams();
  const notify = useToast();
  const [quiz, setQuiz] = useState(null);
  const [answers, setAnswers] = useState({});
  const [result, setResult] = useState(null);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [secondsLeft, setSecondsLeft] = useState(null);
  const submittedRef = useRef(false);

  useEffect(() => {
    let live = true;
    api.get(`/quizzes/${id}`).then(({ data }) => {
      if (!live) return;
      setQuiz(data);
      setSecondsLeft(Math.max(1, data.timeLimitMinutes) * 60);
    }).catch((err) => notify(errorMessage(err), "error")).finally(() => { if (live) setLoading(false); });
    return () => { live = false; };
  }, [id, notify]);

  async function submit() {
    if (submittedRef.current || !quiz) return;
    submittedRef.current = true;
    setSubmitting(true);
    try {
      const payload = { answers: quiz.questions.map((q) => ({ questionId: q.id, selectedOptionId: answers[q.id] || null })) };
      const { data } = await api.post(`/quizzes/${id}/attempt`, payload);
      setResult(data);
      notify(`Quiz submitted · ${data.score}/${data.totalMarks} marks`, data.passed ? "success" : "info");
    } catch (err) {
      submittedRef.current = false;
      notify(errorMessage(err), "error");
    } finally { setSubmitting(false); }
  }

  useEffect(() => {
    if (secondsLeft === null || result || submitting || loading) return;
    const timer = window.setInterval(() => {
      setSecondsLeft((n) => {
        if (n <= 1) {
          window.clearInterval(timer);
          if (!submittedRef.current) setTimeout(() => submit(), 0);
          return 0;
        }
        return n - 1;
      });
    }, 1000);
    return () => window.clearInterval(timer);
  }, [secondsLeft === 0, result, submitting, loading, quiz]);

  const totalMarks = useMemo(() => quiz?.questions?.reduce((sum, q) => sum + q.marks, 0) || 0, [quiz]);
  const answeredCount = Object.keys(answers).length;
  const timeLabel = secondsLeft === null ? "--:--" : `${String(Math.floor(secondsLeft / 60)).padStart(2, "0")}:${String(secondsLeft % 60).padStart(2, "0")}`;

  async function explain(answer) {
    try {
      const { data } = await api.post("/ai/explain", { question: answer.questionText, studentAnswer: answer.studentAnswer || "No answer selected", correctAnswer: answer.correctAnswer });
      setResult((r) => ({ ...r, review: r.review.map((row) => row.questionId === answer.questionId ? { ...row, aiExplanation: data.explanation } : row) }));
      notify(data.usedOpenAi ? "AI explanation generated." : "Explanation ready.", "success");
    } catch (err) { notify(errorMessage(err), "error"); }
  }

  if (loading) return <div className="loading-card"><span className="spinner spinner-dark" /> Loading quiz…</div>;
  if (!quiz) return <div className="panel empty-results"><h3>Quiz unavailable</h3><Link to="/courses" className="btn btn-primary">Back to courses</Link></div>;

  return (
    <div className="quiz-page page-stack">
      <Link to={`/courses/${quiz.courseId}`} className="back-link"><ArrowLeft size={15} /> Back to course</Link>
      <section className="quiz-top-card panel"><div className="quiz-top-icon"><BrainCircuit size={22} /></div><div className="quiz-top-copy"><div className="eyebrow">COURSE ASSESSMENT {quiz.isAiGenerated && <span className="ai-generated-pill"><Sparkles size={12} /> AI generated</span>}</div><h1>{quiz.title}</h1><p>{quiz.description || "Choose the best answer for each question."}</p></div><div className={`quiz-timer ${secondsLeft < 60 ? "quiz-timer-warning" : ""}`}><Clock3 size={17} /><div><small>TIME LEFT</small><b>{result ? "Submitted" : timeLabel}</b></div></div></section>

      {result ? <>
        <section className={`quiz-result-banner ${result.passed ? "result-pass" : "result-review"}`}><div className="result-icon">{result.passed ? <CheckCircle2 size={25} /> : <BrainCircuit size={25} />}</div><div><span>{result.passed ? "Great work" : "Keep practising"}</span><h2>{result.score} <small>/ {result.totalMarks}</small></h2><p>{result.percentage}% · {result.passed ? "You passed this quiz." : "Review the answers and revisit the lesson."} Your mark is saved in your marks ledger.</p></div><Link to="/marks" className="btn btn-outline">View marks <ArrowRight size={15} /></Link></section>
        <div className="review-list">{result.review.map((answer, index) => <article key={answer.questionId} className={`panel review-card ${answer.isCorrect ? "review-correct" : "review-incorrect"}`}><div className="review-question-head"><span className={`review-result-icon ${answer.isCorrect ? "result-icon-good" : "result-icon-bad"}`}>{answer.isCorrect ? <Check size={15} /> : <X size={15} />}</span><div><span className="eyebrow">QUESTION {String(index + 1).padStart(2, "0")}</span><h3>{answer.questionText}</h3></div><span className="review-mark">{answer.marksAwarded}/{quiz.questions[index]?.marks ?? 1}</span></div><div className="review-answers"><div className="answer-line"><small>Your answer</small><b className={answer.isCorrect ? "text-success" : "text-danger"}>{answer.studentAnswer || "No answer selected"}</b></div><div className="answer-line"><small>Correct answer</small><b className="text-success">{answer.correctAnswer}</b></div></div>{answer.explanation && <div className="static-explanation"><Sparkles size={15} />{answer.explanation}</div>}{!answer.isCorrect && <div className="ai-explain-area">{answer.aiExplanation ? <div className="ai-explanation"><span className="ai-generated-pill"><Sparkles size={12} /> AI tutor</span><p>{answer.aiExplanation}</p></div> : <button className="btn btn-outline btn-small" onClick={() => explain(answer)}><Sparkles size={14} /> Explain my answer with AI</button>}</div>}</article>)}</div>
        <div className="quiz-bottom-actions"><Link to={`/courses/${quiz.courseId}`} className="btn btn-outline"><ArrowLeft size={15} /> Back to course</Link><Link to="/dashboard" className="btn btn-primary">My dashboard <ArrowRight size={15} /></Link></div>
      </> : <>
        <div className="quiz-progress-head"><div><b>{answeredCount} of {quiz.questions.length}</b><span> questions answered</span></div><span>{totalMarks} total marks</span></div><div className="quiz-progress-track"><i style={{ width: `${quiz.questions.length ? answeredCount / quiz.questions.length * 100 : 0}%` }} /></div>
        {quiz.questions.map((question, index) => <section className="panel quiz-question-card" key={question.id}><div className="quiz-question-top"><span className="question-number">{String(index + 1).padStart(2, "0")}</span><span className="question-points">{question.marks} {question.marks === 1 ? "mark" : "marks"}</span></div><h3>{question.questionText}</h3><div className="quiz-options">{question.options.map((option) => <label key={option.id} className={`quiz-option ${answers[question.id] === option.id ? "quiz-option-selected" : ""}`}><input type="radio" name={`question-${question.id}`} checked={answers[question.id] === option.id} onChange={() => setAnswers((a) => ({ ...a, [question.id]: option.id }))} /><span className="option-letter">{option.label}</span><span className="option-text">{option.text}</span><span className="option-check"><Check size={15} /></span></label>)}</div></section>)}
        <div className="quiz-submit-bar"><div><b>Ready to submit?</b><small>Your answers are saved when you submit. You can only attempt this quiz once.</small></div><button className="btn btn-primary" disabled={submitting} onClick={submit}>{submitting ? "Submitting…" : "Submit quiz"}<ArrowRight size={16} /></button></div>
      </>}
    </div>
  );
}
