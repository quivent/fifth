class Fifth < Formula
  desc "A Forth for the agentic era"
  homepage "https://github.com/quivent/fifth"
  url "https://github.com/quivent/fifth.git", tag: "v0.1.0"
  license "MIT"

  def install
    system "make", "-C", "engine"
    bin.install "engine/fifth"
  end

  def post_install
    ohai "Setting up ~/.fifth"
    fifth_home = ENV["FIFTH_HOME"] || "#{ENV["HOME"]}/.fifth"
    system "mkdir", "-p", "#{fifth_home}/lib", "#{fifth_home}/packages"
  end

  test do
    assert_match "5", shell_output("#{bin}/fifth -e '2 3 + . cr'")
  end
end
