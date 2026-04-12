from prepare import evaluate_rmse

import pandas as pd
import numpy as np
import statsmodels.api as sm
from sklearn.model_selection import train_test_split

# -------------------------
# Load
# -------------------------
df = pd.read_csv("AmesHousing.csv")
df.columns = df.columns.str.strip().str.replace(" ", "")

# -------------------------
# Target
# -------------------------
df = df[df["SalePrice"] > 0]
y_full = df["SalePrice"]

# -------------------------
# Base Features
# -------------------------
base_features = [
    "OverallQual"
]

X = df[base_features].copy()

# -------------------------
# Train/Validation Split
# -------------------------
X_train, X_val, y_train, y_val = train_test_split(
    X, y_full, test_size=0.2, random_state=42
)

# -------------------------
# Final Model
# -------------------------
X_train = sm.add_constant(X_train[base_features])
X_val = sm.add_constant(X_val[base_features], has_constant="add")

model = sm.OLS(y_train, X_train).fit()

# -------------------------
# Evaluation (test data)
# -------------------------
y_pred = model.predict(X_val)
y_true = y_val

rmse = evaluate_rmse(y_true, y_pred)

print("---")
print("Selected features:", base_features)
print(f"rmse:   {rmse:.3f}")
print(f"r2: {model.rsquared:.3f}")