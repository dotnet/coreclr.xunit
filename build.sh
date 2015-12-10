#!/bin/bash

if ! [ -x "$(command -v mono)" ]; then
  echo >&2 "Could not find 'mono' on the path."
  exit 1
fi

echo ""
echo "Installing .NET Execution Environment..."
echo ""

. tools/dnvm.sh
dnvm install 1.0.0-rc2-16177 -r coreclr -u
if [ $? -ne 0 ]; then
  echo >&2 ".NET Execution Environment installation has failed."
  exit 1
fi

dnvm install 1.0.0-rc2-16177 -u
if [ $? -ne 0 ]; then
  echo >&2 ".NET Execution Environment installation has failed."
  exit 1
fi

echo ""
echo "Restoring packages..."
echo ""

dnvm use 1.0.0-rc2-16177
dnu restore
if [ $? -ne 0 ]; then
  echo >&2 "Package restore has failed."
  exit 1
fi

echo ""
echo "Building packages..."
echo ""

dnu pack src/xunit.runner.dnx
if [ $? -ne 0 ]; then
  echo >&2 "Build packages has failed."
  exit 1
fi

echo ""
echo "Running tests..."
echo ""
dnx -p test/test.xunit.runner.dnx test -parallel none
if [ $? -ne 0 ]; then
  echo >&2 "Running tests on Mono has failed."
  exit 1
fi

dnvm use 1.0.0-rc2-16177 -r coreclr
dnx -p test/test.xunit.runner.dnx test -parallel none
if [ $? -ne 0 ]; then
  echo >&2 "Running tests on CoreCLR has failed."
  exit 1
fi
