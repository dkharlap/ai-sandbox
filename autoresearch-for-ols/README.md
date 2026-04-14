# Autoresearch for OLS — Ames Housing Price Prediction

## Background

As part of an extremely talented cohort in the **Statistics for Data Science** course at the **University of Waterloo**, our group tackled the [Ames Housing Dataset](https://www.kaggle.com/datasets/shashanknecrothapa/ames-housing-dataset) from Kaggle.

For our group assignment, we focused on **Ordinary Least Squares (OLS)** regression to predict house prices. Four team members each built their own models, experimenting with different input variables, feature engineering, and transformations. Our best result, achieved through combined iteration, was:

| Metric | Value |
|--------|-------|
| Adj. R² | ~0.858 |
| Test R² | ~0.900 |
| RMSE | ~26,646 |

This model used just **4 variables** and stayed simple and interpretable.

---

## The Autoresearch Experiment

After grinding through many manual iterations, I got curious: how far could the model be pushed *automatically* using [Andrej Karpathy's Autoresearch](https://github.com/karpathy/autoresearch)?

Turns out, pretty far. In just a few hours I ran roughly **100 experiments**, most of them suggested by the agentic AI (OpenAI Codex), with occasional nudges from me. The best model came out at:

| Metric | Value | vs. Human Model |
|--------|-------|-----------------|
| Adj. R² | ~0.846 | — |
| Test R² | ~0.950 | **~6% better** |
| RMSE | ~19,146 | **~28% better** |

The methodology stayed OLS throughout, but the resulting code ended up roughly **2–3× more complex** than the hand-crafted version — still totally readable and maintainable, just a lot more going on under the hood.

---

## Try It Yourself

1. Follow the setup guide in [Andrej Karpathy's Autoresearch repo](https://github.com/karpathy/autoresearch) - it's quick and painless.
2. Once you're set up, kick things off with:

```
Hi, have a look at program.md and let's kick off a new experiment! Let's do the setup first.
```

---

## Results

The [`results/`](https://github.com/dkharlap/ai-sandbox/tree/main/autoresearch-for-ols/results) folder has everything from my session:

| File | Description |
|------|-------------|
| [`train.py`](https://github.com/dkharlap/ai-sandbox/blob/main/autoresearch-for-ols/results/train.py) | Final OLS model produced by Autoresearch |
| [`results.tsv`](https://github.com/dkharlap/ai-sandbox/blob/main/autoresearch-for-ols/results/results.tsv) | Full experiment log — which runs were kept and which were rejected |
| [`terminal-session-output.txt`](https://github.com/dkharlap/ai-sandbox/blob/main/autoresearch-for-ols/results/terminal-session-output.txt) | Complete communication log with OpenAI Codex throughout the session |
