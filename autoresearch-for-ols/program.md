# autoresearch

This is an experiment to have the LLM do its own research on optimizing OLS approach to predict housing prices.

## Experimentation

You launch it simply as: `uv run train.py`.

**What you CAN do:**
- Modify `train.py` — this is the only file you edit. Everything is fair game as long as you use Ordinary Least Squares (OLS): feature engineering (interactions, polynomials, age), log target, parameter transformation, median imputation, excluding outliers, feature selection, or anything else you may come up with.

**What you CANNOT do:**
- Modify `prepare.py`. It is read-only. It contains the fixed evaluation.
- Modify the evaluation harness. The `evaluate_rmes` function in `prepare.py` is the ground truth metric.
- Modify how model is trained and tested. It should be two separate sets of data without overlaps.

**The goal is simple: get the lowest rmes.** 

**Simplicity criterion**: All else being equal, simpler is better. A small improvement that adds ugly complexity is not worth it. Conversely, removing something and getting equal or better results is a great outcome — that's a simplification win. When evaluating whether to keep a change, weigh the complexity cost against the improvement magnitude. Do not include improvements that deliver less than 500 rmes improvements and add code. Any rmes improvement from deleting code? Definitely keep. An improvement of ~0 but much simpler code? Keep.

**The first run**: Your very first run should always be to establish the baseline, so you will run the training script as is.

## Output format

Once the script finishes it prints a summary like this:

```
---
rmse:   38732.256
r2: 0.765
```

## Logging results

When an experiment is done, log it to `results.tsv` (tab-separated, NOT comma-separated — commas break in descriptions).

The TSV has a header row and 5 columns:

```
rmse	r2  status	description
```

1. rmse achieved (e.g. 1.234567) — use 0.000000 for crashes
2. r2 achieved
3. status: `keep`, `discard`, or `crash`
4. short text description of what this experiment tried

Example:

```
rmse        r2	    status	description
38732.256   0.765	keep	baseline
33232.256	0.824   keep	increase LR to 0.04
48732.256	0.723   discard	switch to GeLU activation
```

## The experiment loop

LOOP FOREVER:

1. Tune `train.py` with an experimental idea by directly hacking the code.
2. Run the experiment: `uv run train.py > run.log 2>&1` (redirect everything — do NOT use tee or let output flood your context)
3. Read out the results: `grep "^rmse:\|^r2:" run.log`
4. If the grep output is empty, the run crashed. Run `tail -n 50 run.log` to read the Python stack trace and attempt a fix. If you can't get things to work after more than a few attempts, give up.
5. Record the results in the tsv
6. If rmse improved (lower) by at least 300, you keep the changes in train.py
7. If rmse is not improved (lower) by at least 300, you revert the changes to original

The idea is that you are a completely autonomous researcher trying things out. If they work, keep. If they don't, discard. And you're advancing the code so that you can iterate. If you feel like you're getting stuck in some way, you can rewind but you should probably do this very very sparingly (if ever).

**Timeout**: Each experiment should take ~25 seconds total (+ a few seconds for startup and eval overhead). If a run exceeds 10 seconds, kill it and treat it as a failure (discard and revert).

**Crashes**: If a run crashes (OOM, or a bug, or etc.), use your judgment: If it's something dumb and easy to fix (e.g. a typo, a missing import), fix it and re-run. If the idea itself is fundamentally broken, just skip it, log "crash" as the status in the tsv, and move on.

**NEVER STOP**: Once the experiment loop has begun (after the initial setup), do NOT pause to ask the human if you should continue. Do NOT ask "should I keep going?" or "is this a good stopping point?". The human might be asleep, or gone from a computer and expects you to continue working *indefinitely* until you are manually stopped. You are autonomous. If you run out of ideas, think harder — read papers referenced in the code, re-read the in-scope files for new angles, try combining previous near-misses, try more radical architectural changes. The loop runs until the human interrupts you, period.

As an example use case, a user might leave you running while they sleep. You can run approx 12/hour, for a total of about 100 over the duration of the average human sleep. The user then wakes up to experimental results, all completed by you while they slept!
