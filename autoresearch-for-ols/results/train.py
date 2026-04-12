from prepare import evaluate_rmse

import numpy as np
import pandas as pd
import patsy
import statsmodels.api as sm
from sklearn.model_selection import train_test_split

# -------------------------
# Load
# -------------------------
df = pd.read_csv("AmesHousing.csv")
df.columns = df.columns.str.strip().str.replace(" ", "")
df["LogGrLivArea"] = np.log1p(df["GrLivArea"])
df["LogTotalBsmtSF"] = np.log1p(df["TotalBsmtSF"])
df["LogLotArea"] = np.log1p(df["LotArea"])

# -------------------------
# Target
# -------------------------
df = df[df["SalePrice"] > 0].copy()
y_full = np.log1p(df["SalePrice"])

# -------------------------
# Features
# -------------------------
numeric_features = [
    "OverallQual",
    "LogGrLivArea",
    "LogTotalBsmtSF",
    "GarageCars",
    "YearBuilt",
    "LogLotArea",
    "OverallCond",
    "Fireplaces",
]

categorical_features = [
    "BsmtQual",
    "SaleType",
    "Neighborhood",
    "KitchenQual",
    "BsmtExposure",
    "BsmtFinType1",
    "BldgType",
]

X = df[numeric_features + categorical_features].copy()

# -------------------------
# Train/Validation Split
# -------------------------
X_train, X_val, y_train, y_val = train_test_split(
    X, y_full, test_size=0.2, random_state=42
)

# -------------------------
# Train-only preprocessing
# -------------------------
X_train_num = X_train[numeric_features].copy()
X_val_num = X_val[numeric_features].copy()

numeric_medians = X_train_num.median()
X_train_num = X_train_num.fillna(numeric_medians)
X_val_num = X_val_num.fillna(numeric_medians)

pre_regime = X_train_num["OverallQual"] >= 8
val_pre_regime = X_val_num["OverallQual"] >= 8
cap_quantiles = {"LogTotalBsmtSF": 0.99, "LogLotArea": 0.985}
for column, quantile in cap_quantiles.items():
    low_cap = X_train_num.loc[~pre_regime, column].quantile(quantile)
    high_cap = X_train_num.loc[pre_regime, column].quantile(quantile)
    X_train_num.loc[~pre_regime, column] = X_train_num.loc[~pre_regime, column].clip(upper=low_cap)
    X_train_num.loc[pre_regime, column] = X_train_num.loc[pre_regime, column].clip(upper=high_cap)
    X_val_num.loc[~val_pre_regime, column] = X_val_num.loc[~val_pre_regime, column].clip(upper=low_cap)
    X_val_num.loc[val_pre_regime, column] = X_val_num.loc[val_pre_regime, column].clip(upper=high_cap)

spline_formula = "bs(LogGrLivArea, df=4, degree=3, include_intercept=False) - 1"
spline_train = patsy.dmatrix(spline_formula, X_train_num, return_type="dataframe")
spline_val = patsy.build_design_matrices(
    [spline_train.design_info], X_val_num, return_type="dataframe"
)[0]

X_train_num = pd.concat([X_train_num.drop(columns=["LogGrLivArea"]), spline_train], axis=1)
X_val_num = pd.concat([X_val_num.drop(columns=["LogGrLivArea"]), spline_val], axis=1)

X_train_cat = X_train[categorical_features].copy().fillna("Missing")
X_val_cat = X_val[categorical_features].copy().fillna("Missing")

X_train_cat = pd.get_dummies(X_train_cat, drop_first=True, dtype=float)
X_val_cat = pd.get_dummies(X_val_cat, drop_first=True, dtype=float)
X_val_cat = X_val_cat.reindex(columns=X_train_cat.columns, fill_value=0.0)

X_train_final = pd.concat([X_train_num, X_train_cat], axis=1)
X_val_final = pd.concat([X_val_num, X_val_cat], axis=1)
train_regime = X_train_num["OverallQual"] >= 8
val_regime = X_val_num["OverallQual"] >= 8

# -------------------------
# Final Model
# -------------------------
X_train_low = sm.add_constant(X_train_final.loc[~train_regime], has_constant="add")
X_train_high = sm.add_constant(X_train_final.loc[train_regime], has_constant="add")
X_val_low = sm.add_constant(X_val_final.loc[~val_regime], has_constant="add")
X_val_high = sm.add_constant(X_val_final.loc[val_regime], has_constant="add")

model_low = sm.OLS(y_train.loc[~train_regime], X_train_low).fit()
model_high = sm.OLS(y_train.loc[train_regime], X_train_high).fit()
smear_low = np.exp(model_low.resid).mean()
smear_high = np.exp(model_high.resid).mean()

# -------------------------
# Evaluation (test data)
# -------------------------
y_pred = pd.Series(index=y_val.index, dtype=float)
y_pred.loc[~val_regime] = np.expm1(model_low.predict(X_val_low)) * smear_low
y_pred.loc[val_regime] = np.expm1(model_high.predict(X_val_high)) * smear_high
rmse = evaluate_rmse(np.expm1(y_val), y_pred)

print("---")
print("Numeric features:", numeric_features)
print("Categorical features:", categorical_features)
print("Low quality train rows:", int((~train_regime).sum()))
print("High quality train rows:", int(train_regime.sum()))
print("Target transform: log1p(SalePrice) with regime-specific smearing correction")
print(f"rmse:   {rmse:.3f}")
print(f"r2: {(model_low.rsquared + model_high.rsquared) / 2:.3f}")
